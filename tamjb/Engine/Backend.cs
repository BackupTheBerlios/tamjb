/// \file
/// $Id$
///
/// The jukebox player back end.
///

// Copyright (C) 2004 Tom Surace.
//
// This file is part of the Tam Jukebox project.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Contacts:
//
//   Tom Surace <tekhedd@byteheaven.net>

namespace byteheaven.tamjb.Engine
{
   using System;
   using System.Collections;
   using System.Data;
   using System.Diagnostics;
   using System.IO;
   using System.Threading;
   using System.Xml.Serialization;

   using byteheaven.tamjb.Interfaces;
   using byteheaven.tamjb.SimpleMp3Player;

   ///
   /// The main jukebox player object.
   ///
   public class Backend
      : IEngine, 
        IBackend
   {
      ///
      /// Potential quality settings for the backend. 
      ///
      public enum Quality
      {
         ///
         /// Poor audio quality. Crossovers are not very good. 
         ///
         LOW,

	 ///
	 /// It's in the middle somewhre.
	 ///
	 MEDIUM,

         ///
         /// Highest audio quality. Multi-band compression with 
         /// large IIR crossover filters, limiting, soft filtering: 
         /// the works.
         ///
         HIGH
      }

      public enum CompressionType
      {
         SIMPLE,                ///< Single band compression
         MULTIBAND,             ///< Multi band compression
         // NONE - not implemented? :)
      }

      ///
      /// get/set the database connection string (as an url, must
      /// be file:something for SQLite.
      ///
      public string connectionString
      {
         set
         {
            _connectionString = value;
         }
         get
         {
            return _connectionString;
         }
      }

      ///
      /// get/set the default queue size for new engines
      ///
      public int desiredQueueSize
      {
         set
         {
            _desiredQueueSize = value;
         }
         get
         {
            return _desiredQueueSize;
         }
      }
      
      ///
      /// sets the size in samples for all channels
      ///
      /// Takes effect when the next song starts.
      ///
      public uint bufferSize
      {
         get
         {
            return _player.bufferSize;
         }
         set
         {
            _player.bufferSize = value;
         }
      }

      ///
      /// how many audio buffers to have in queue
      ///
      /// Takes effect when playback is stopped/started
      /// 
      public uint bufferCount
      {
         get
         {
            return _player.buffersInQueue;
         }
         set
         {
            _player.buffersInQueue = value;
         }
      }

      ///
      /// how many bufers to load before starting playback
      ///
      /// Should be same or less than bufferCount
      ///
      public uint bufferPreload 
      {
         get
         {
            return _player.buffersToPreload;
         }
         set
         {
            _player.buffersToPreload = value;;
         }
      }

      ///
      /// Singleton accessor
      ///
      static public Backend theBackend
      {
         get
         {
            Debug.Assert( null != _theBackend, "Forgot to call Init?" );
            return _theBackend;
         }
      }

      ///
      /// Call this to create the singleton object
      ///
      public static void Init( int desiredQueueSize,
                               string connectionString,
                               Quality qualityLevel,
                               CompressionType compressType )
      {
         _theBackend = new Backend( desiredQueueSize, 
                                    connectionString,
                                    qualityLevel,
                                    compressType );
      }

      ///
      /// \warning When creating an object for remoting, call Init() and
      ///   access the singleton returned by theBackend. This constructor
      ///   is public only for use by non-remoting configurations.
      /// 
      public Backend( int desiredQueueSize, 
                      string connectionString,
                      Quality qualityLevel,
                      Backend.CompressionType compressionType )
      {
         _Trace( "[Backend]" );

         _qualityLevel = qualityLevel;
         _compressionType = compressionType;
         _desiredQueueSize = desiredQueueSize;
         _connectionString = connectionString;

         if (null == _connectionString)
            throw new ApplicationException( "Engine is not properly initialized by the server" );

         _database = new StatusDatabase( _connectionString );

         // Todo: initialize compressor from stored settings in database
         _compressor = _LoadCompressor();

         // Get defaults from this kind of thing?
         // string bufferSize = 
         //    ConfigurationSettings.AppSettings ["BufferSize"];
                        
         // Set up default buffering for the audio engine
         _player = new Player();
         _player.bufferSize = 44100 / 8 ;
         _player.buffersInQueue = 40;
         _player.buffersToPreload = 20;

         // Set up callbacks
         _player.OnTrackFinished +=
            new TrackFinishedHandler( _TrackFinishedCallback );

         _player.OnTrackPlayed +=
            new TrackStartingHandler( _TrackStartingCallback );
            
         _player.OnReadBuffer +=
            new ReadBufferHandler( _TrackReadCallback );

         // Start playing as the default user, if there is one:
         try
         {
            _database.GetController( out _controllingUser,
                                     out _controllingMood );
         }
         catch
         {
         }

         if (null == _controllingUser)
            _controllingUser = new Credentials();

         if (null == _controllingMood)
            _controllingMood = new Mood();
      }
      
      ~Backend()
      {
         _Trace( "[~Backend]" );

         // Ensure this is stopped, or the app may not
         // really exit
         ShutDown();

         // System.Runtime.Remoting.RemotingServices.Disconnect(this);
         _database = null;
      }

      public void ShutDown()
      {
         // Don't hold the lock, or the callbacks into this object might
         // deadlock.
         _player.ShutDown();
      }


      //
      // Get the current mood. May return null for either or both
      // if no current user or mood exists.
      //
      public void GetCurrentUserAndMood( ref Credentials cred,
                                         ref Mood mood )
      {
         lock (_serializer)
         {
            cred = _controllingUser;
            mood = _controllingMood;
         }
      }

      ///
      /// Updates the state and returns true if it has changed
      ///
      /// \note This is totally inefficient for remoting. 
      ///
      public bool CheckState( ref EngineState state )
      {
         lock (_serializer)
         {
            if (null == state)
               throw new ArgumentException( "may not be null", "state" );

            // No change? nothing to do!
            if (((EngineState)state).changeCount == _changeCount)
               return false;

            _Trace( "CheckState: change occurred" );

            state.isPlaying = _shouldBePlaying;
            state.currentTrackIndex = _playQueueCurrentTrack;
            state.playQueue = 
               (ITrackInfo[])_playQueue.ToArray(typeof(PlayableData));

            state.changeCount = _changeCount;
//             state = new EngineState( _shouldBePlaying,
//                                      _playQueueCurrentTrack,
//                                      _playQueue,
//                                      _changeCount );
            return true;
         }
      }

      ///
      /// Implements IEngine.GetState()
      ///
      public EngineState GetState()
      {
         lock (_serializer)
         {
            // Note: when remoting, this is probably MORE efficient than
            // the CheckState method. Go figure!
            return new EngineState( 
               _shouldBePlaying,
               _playQueueCurrentTrack,
               (ITrackInfo[])_playQueue.ToArray(typeof(PlayableData)),
               _changeCount );
         }
      }

      public long changeCount
      {
         get
         {
            lock (_serializer)
            {
               return _changeCount;
            }
         }
      }


      ///
      /// Call this on, say, a half-second interval to ensure
      /// that the queue is always full and to poll for new
      /// files in the file root directory. (ies?)
      ///
      /// \todo This should only really be called by the Server code. 
      ///   Create a separate interface (IServerEngine?) for this and
      ///   don't export that?
      ///
      /// Yes, I admit that polling is lame. But it works great with
      /// a minimum of effort.
      ///
      public void Poll()
      {
         // _Trace( "[Poll]" );
         lock (_serializer)
         {
            try
            {
               // Enqueue at most one song each time this is polled
               if (unplayedTrackCount < _desiredQueueSize)
                  EnqueueRandomSong();

               // If playback has stopped, restart it now.
               if (_shouldBePlaying && ( ! _player.isPlaying ))
               {
                  _Trace( "Hey, we are not playing. Restarting.." );
                  GotoNext( true );
               }
            }
            catch (Exception e)
            {
               _Trace( "Exception propagating from Poll()" );
               _Trace( e.ToString() );
               throw;
            }
         }
      }

      ///
      /// Create a new database in the given file (connection whatever)
      ///
      public void CreateDatabase( string connectionString )
      {
         StatusDatabase db = new StatusDatabase( connectionString );
         db.CreateTablesIfNecessary();
      }


      ///
      /// Add a song at the end of the play queue.
      ///
      /// \return true if a song was enqueued, false if no songs
      ///   were found.
      ///
      bool EnqueueRandomSong()
      {
         _Trace( "[EnqueueRandomSong]" );

         ++_changeCount;

         // Pick a song and go.
         PlayableData nextTrack;
         uint count = _database.PickRandom( _rng, out nextTrack );

         if (0 == count)
         {
            _Trace( "  Playlist Empty: No songs found" );
            return false;    // No songs found
         }

         _PlaylistAppend( nextTrack );

         return true;
      }

      ///
      /// Increases suck by 50% of the difference between its
      /// current level and 100%. Thus it theoretically never reaches
      /// 100% (cause integer math rounds down).
      ///
      public void IncreaseSuckZenoStyle( Credentials cred,
                                         uint trackKey )
      {
         _Trace( "[IncreaseSuckZenoStyle]" );

         lock (_serializer)
         {
            ++_changeCount;

            uint level = _database.GetSuck( cred.id, trackKey );

            // The database shouldn't contain invalid attributes, right?
            Debug.Assert( level <= 10000 && level >= 0 );

            // how much is halfway from the current level to 100%?
            // (so: if it was 10%, it's now 10 + 90/2 = 55%.)

            uint difference = 10000 - level;
            level += ( difference / 2 ); 

            // Is our math correct?
            Debug.Assert( level <= 10000 && level >= 0 );

            _database.SetSuck( cred.id, trackKey, level );
         }
      }

      public void DecreaseSuckZenoStyle( Credentials cred,
                                         uint trackKey )
      {
         _Trace( "[DecreaseSuckZenoStyle]" );

         lock (_serializer)
         {
            ++_changeCount;
            uint level = _database.GetSuck( cred.id, trackKey );

            // The database shouldn't contain invalid attributes, right?
            Debug.Assert( level <= 10000 && level >= 0 );

            // how much is halfway from the current level to 100%?
            // (so: if it was 10%, it's now 10 + 90/2 = 55%.)

            level /= 2;

            _database.SetSuck( cred.id, trackKey, level );
         }
      }

      public void IncreaseAppropriateZenoStyle( Credentials cred,
                                                Mood mood,
                                                uint trackKey )
      {
         _Trace( "[IncreaseAppropriateZenoStyle]" );

         lock (_serializer)
         {
            ++_changeCount;

            uint level = _database.GetAppropriate( cred.id, 
                                                   mood.id, 
                                                   trackKey );

            // The database shouldn't contain invalid attributes, right?
            Debug.Assert( level <= 10000 && level >= 0 );

            // how much is halfway from the current level to 100%?
            // (so: if it was 10%, it's now 10 + 90/2 = 55%.)

            uint difference = 10000 - level;
            level += ( difference / 2 ); 

            // Is our math correct?
            Debug.Assert( level <= 10000 && level >= 0 );

            _database.SetAppropriate( cred.id, mood.id, trackKey, level );
         }
      }

      public void DecreaseAppropriateZenoStyle( Credentials cred,
                                                Mood mood,
                                                uint trackKey )
      {
         _Trace( "[DecreaseAppropriateZenoStyle]" );

         lock (_serializer)
         {
            ++_changeCount;
            uint level = _database.GetAppropriate( cred.id, 
                                                   mood.id,
                                                   trackKey );

            // The database shouldn't contain invalid attributes, right?
            Debug.Assert( level <= 10000 && level >= 0 );

            // how much is halfway from the current level to 100%?
            // (so: if it was 10%, it's now 10 + 90/2 = 55%.)

            level /= 2;

            _database.SetAppropriate( cred.id, 
                                      mood.id,
                                      trackKey, 
                                      level );
         }
      }


      public bool isPlaying
      {
         get
         {
            return _shouldBePlaying;
         }
      }

      public ITrackInfo currentTrack
      {
         ///
         /// \return the current track's info or null if no tracks
         ///   are in the queue
         ///
         get
         {
            lock (_serializer)
            {
               return (ITrackInfo)_PlaylistGetCurrent();
            }
         }
      }

      ///
      /// May be -1 if not playing.
      //
      public int currentTrackIndex
      {
         get
         {
            return _playQueueCurrentTrack;
         }
      }

      public int unplayedTrackCount
      {
         get
         {
            lock (_serializer)
            {
               return _PlaylistGetCount() - _playQueueCurrentTrack;
            }
         }
      }

      ///
      /// Indexer allows the engine to be treated as an array of playable
      /// data. Huh.
      ///
      public ITrackInfo this [int index]
      {
         ///
         /// \return null if index is out of range
         ///
         get
         {
            lock (_serializer)
            {
               return (ITrackInfo)_PlaylistGetAt( index );
            }
         }
      }

      public int Count
      {
         get
         {
            lock (_serializer)
            {
               return _PlaylistGetCount();
            }
         }
      }

      public ITrackInfo GetFileInfo( uint key )
      {
         lock (_serializer)
         {
            return (ITrackInfo)_database.GetFileInfo( key );
         }
      }

      ///
      /// IEngine interfaces
      ///
      public void GetAttributes( Credentials cred,
                                 Mood mood,
                                 uint trackKey,
                                 out double suck,
                                 out double appropriate )
      {
         lock (_serializer)
         {
            suck = _database.GetSuck( cred.id, trackKey );
            appropriate = _database.GetAppropriate( cred.id, 
                                                    mood.id, 
                                                    trackKey );
         }
      }

      ///
      /// GotoFile function with credentials for future expansion.
      ///
      public void GotoNextFile( Credentials cred, uint currentTrackKey )
      {
         _Trace( "[GotoNextFile]" );

         lock (_serializer)
         {
            GotoNext( true );
         }
      }

      ///
      /// Set the next track in the mp3 player, and optionally start it
      /// right now.
      ///
      void GotoNext( bool startNow )
      {
         _Trace( "[GotoNext]" );

         ++_changeCount;

         //
         // Loop to keep advancing until we reach a track that's worthy
         //
         while (true)           
         {
            PlayableData nextFile = _PlaylistGoNext();
            if (null == nextFile)
            {
               _player.Stop(); 
               break;           // nothing to play, break out.
            }

            // If this track is cool, we are done.
            if (_WantToPlayTrack(nextFile) )
            {
               _Trace( "  NEXT = " + nextFile.title );
               if (startNow)
                  _player.PlayFile( nextFile.filePath, nextFile.key );
               else
                  _player.SetNextFile( nextFile.filePath, nextFile.key );

               _shouldBePlaying = true;
               break;
            }
            else
            {
               _Trace( "  REJECTED = " + nextFile.title );
            }
         }
      }

      ///
      /// If the current track is rejected, this calls GotoNext
      ///
      public void ReevaluateCurrentTrack()
      {
         _Trace( "[Reevaluate]" );

         lock (_serializer)
         {
            PlayableData currentTrack = _PlaylistGetCurrent();
            if (null != currentTrack
                && (!_WantToPlayTrack(currentTrack)))
            {
               GotoNext( true );
            }
         }
      }

      public void GotoPrevFile( Credentials cred, uint currentTrackKey )
      {
         _Trace( "[GotoPrevFile]" );

         lock (_serializer)
         {
            GotoPrev();
         }
      }

      ///
      /// Push this file onto the queue of songs to play and start
      /// playing it
      ///
      void GotoPrev()
      {
         _Trace( "[GotoPrev]" );
         ++_changeCount;

         PlayableData prevFile = _PlaylistGoPrev();
         _Trace( "  PREV = " + prevFile.title );
         if (null != prevFile)
         {
            _player.PlayFile( prevFile.filePath, prevFile.key );
            _shouldBePlaying = true;
         }
      }


      ///
      /// Called by the mp3 reader when a track is finished. 
      ///
      /// \warning Called in the context of the reader thread!
      ///
      void _TrackFinishedCallback(  TrackFinishedInfo info )
      { 
         _Trace( "[_TrackFinishedCallback]" );
         ++ _changeCount;

         // Previous track could be nothing?
         if (null != info)
         {            
            Trace.WriteLine( 
               String.Format(
                  "Track {0} finished. status:{1}",
                  info.key,
                  info.reason ) );

            // Because of the threading, I don't know we can guarantee
            // this will never happen. Let's find out:
            Debug.Assert( info.key == currentTrack.key,
                          "finished track != playing track" );

            switch (info.reason)
            {
            case TrackFinishedInfo.Reason.NORMAL:
            case TrackFinishedInfo.Reason.USER_REQUEST:
            case TrackFinishedInfo.Reason.PLAY_ERROR: // What to do with this?
               break;

            case TrackFinishedInfo.Reason.OPEN_ERROR: // File probably missing
               // Ouch, potential deadlock.
               lock (_serializer)
               {
                  // Update the ref to the current-playing track data
                  PlayableData currentInfo = _PlaylistGetCurrent();
                  currentInfo.status = TrackStatus.MISSING;

                  _database.SetTrackStatus
                     ( info.key, 
                       StatusDatabase.TrackStatus.MISSING );
               }
               break;

            default:
               throw new ApplicationException( "unexpected case in switch" );
            }
         }

         if (_shouldBePlaying)  // don't bother if we should be stopped
         {
            // Don't acquire the _serializer lock here, because this could
            // cause a deadlock!

            // Advance the playlist (_Playlist* functions are threadsafe)
            GotoNext( false );
         }
      }
   
      ///
      /// Called by the mp3 reader when a track starts playing.
      ///
      /// \warning Called in the context of the reader thread!
      ///
      void _TrackStartingCallback( uint index, string path )
      {
         // This would be a good place to update our current-playing-track
         // indicator.
         ++ _changeCount;
      }

      ///
      /// Called for each buffer in every track.
      ///
      /// Here we can process the audio.
      ///
      void _TrackReadCallback( byte [] buffer, int length )
      {
         _audioMutex.WaitOne(); 
         try
         {
            Debug.Assert( length % 4 == 0, 
                          "I could have sworn this was 16 bit stereo" );
            
            if (length < 4)
               return;

            int offset = 0;
            while (offset + 3 < length)
            {
               
               double left = (((long)(sbyte)buffer[offset + 1] << 8) |
                              ((long)buffer[offset] ));
               
               double right = (((long)(sbyte)buffer[offset + 3] << 8) |
                               ((long)buffer[offset + 2] ));

               left += _denormalFix;
               right += _denormalFix;
               _denormalFix = - _denormalFix;

#if WATCH_DENORMALS
               // it is assumed that no plugin sends denormal values
               // as its output (well, that's how it should be, yes?)
               // so it may be assumed that NONE of them check their input.
               // So, let's fix it here:
               Denormal.CheckDenormal( "Backend input left", 
                                       left );
               Denormal.CheckDenormal( "Backend input right", 
                                       right );
#endif

               _compressor.Process( ref left, ref right );

               long sample = (long)left;
               buffer[offset]     = (byte)(sample & 0xff);
               buffer[offset + 1] = (byte)(sample >> 8);

               sample = (long)right;
               buffer[offset + 2] = (byte)(sample & 0xff);
               buffer[offset + 3] = (byte)(sample >> 8);

               offset += 4;
            }
         }
         finally
         {
            _audioMutex.ReleaseMutex();
         }
      }

      //
      // Attack ratio per-sample. Hmmm.
      //
      public double compressAttack
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).compressAttack;
         }

         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).compressAttack = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      public double compressDecay
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).compressDecay;
         }
         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).compressDecay = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public int compressThresholdBass
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).compressThresholdBass;
         }
         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).compressThresholdBass =
                     value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      public int compressThresholdMid
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).compressThresholdMid;
         }
         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).
                     compressThresholdMid = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      public int compressThresholdTreble
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).compressThresholdTreble;
         }
         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).
                     compressThresholdTreble = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      public bool learnLevels
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).doAutomaticLeveling;
         }
         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).
                     doAutomaticLeveling = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      ///
      /// Input level gate threshold (0-32767). Probably should be
      /// less than the compress threshold. :)
      ///
      public int gateThreshold
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).gateThreshold;
         }
         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).gateThreshold = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public double compressRatio
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).compressRatio;
         }
         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).compressRatio = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      public uint compressPredelayMax
      {
         get
         {
            return Compressor.MAX_PREDELAY;
         }
      }

      ///
      /// Compress predelay (in milliseconds. Should not be more than
      /// compressPredelayMax, or less than 0).
      ///
      public uint compressPredelay
      {
         get
         {
            return ((IMultiBandCompressor)_compressor).compressPredelay;
         }
         set
         {
            lock (_serializer)
            {
               _audioMutex.WaitOne();
               try
               {
                  ((IMultiBandCompressor)_compressor).compressPredelay = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
         }
      }

      ///
      /// Save the current compression settings
      ///
      void _StoreCompressSettings()
      {
         Debug.Assert( null != _compressor );

         // Create a serializer, and serialize to rom
         XmlSerializer serializer = 
            new XmlSerializer( typeof(IMultiBandCompressor) );
         
         StringWriter str = new StringWriter();
         serializer.Serialize( str, _compressor );
         
         _database.StoreCompressSettings( str.ToString() );
      }

      ///
      /// load a compressor using the settings in the database, or a default
      /// one if the settings are not found/valid
      ///
      IAudioProcessor _LoadCompressor()
      {
         IAudioProcessor compressor = null;
         try
         {
            Type compressorType;
            switch (_compressionType)
            {
            case CompressionType.SIMPLE:
               // Note: the poor compressor is actually pretty bitchin
               compressorType = typeof( PoorCompressor );
               break;

            case CompressionType.MULTIBAND:
               compressorType = typeof( MultiBandCompressor );
               break;

            default:
               throw new ApplicationException( "Unexpected quality level" );
            }

            string settings = _database.GetCompressSettings();
            if (null != settings)
            {
               XmlSerializer serializer = new XmlSerializer( compressorType );

               StringReader str = new StringReader( settings );
               compressor = 
                  (IAudioProcessor)serializer.Deserialize( str );
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }

         if (null == compressor)
         {
            _Trace( "Note: compression settings not found, using defaults" );

            // Depending on quality settings, choose:
            if (_compressionType == CompressionType.MULTIBAND)
               compressor = new MultiBandCompressor( _qualityLevel );
            else
               compressor = new PoorCompressor();
         }
         
         return compressor;
      }

      ///
      /// Get the current playing track's info
      ///
      PlayableData _PlaylistGetCurrent()
      {
         _playQueueMutex.WaitOne();
         try
         {
            if (_playQueue.Count <= _playQueueCurrentTrack 
                || _playQueueCurrentTrack < 0)
            {
               return null;
            }

            return (PlayableData)_playQueue[_playQueueCurrentTrack];
         }
         finally
         {
            _playQueueMutex.ReleaseMutex();
         }
      }

      PlayableData _PlaylistGetAt( int index )
      {
         _playQueueMutex.WaitOne();
         try
         {
            if (_playQueue.Count <= index)
               return null;

            return (PlayableData)_playQueue[index];
         }
         finally
         {
            _playQueueMutex.ReleaseMutex();
         }
      }

      ///
      /// \note Thread safe. Probably. :)
      ///
      PlayableData _PlaylistGoNext()
      {
         _playQueueMutex.WaitOne();
         try
         {
            Debug.Assert( _playQueueCurrentTrack >= -1,
                          "-1 indicates no previously-played tracks" );
            
            int left = _playQueue.Count - _playQueueCurrentTrack - 1;
            if (left <= 0)
               return null;
            
            // If we've got all the played-song history we want,
            //   Pop the head off the list and throw it away
            // Otherwise
            //   Just increment the offset
            
            if (_playQueueCurrentTrack >= _maxFinishedPlaying)
               _PlaylistPop();

            ++ _playQueueCurrentTrack;
            ++ _trackCounter;

            return (PlayableData)_playQueue[_playQueueCurrentTrack];
         }
         finally
         {
            _playQueueMutex.ReleaseMutex();
         }
      }

      ///
      /// \note Thread safe. Probably. :)
      ///
      PlayableData _PlaylistGoPrev()
      {
         _playQueueMutex.WaitOne();
         try
         { 
            // loop to go back until we find a track that isn't missing
            // or damaged, otherwise it will be impossible to go back:
            while (true)        
            {
               if (_playQueueCurrentTrack <= 0)
                  return null;

               -- _playQueueCurrentTrack;
               -- _trackCounter;

               PlayableData data = 
                  (PlayableData)_playQueue[_playQueueCurrentTrack];

               if (data.status == TrackStatus.OK)
                  return data;  // ** Yeah, this one will do! **
            }
         }
         finally
         {
            _playQueueMutex.ReleaseMutex();
         }
      }

      ///
      /// Add a track to the end of the playlist. Duh. :)
      ///
      void _PlaylistAppend( PlayableData trackData )
      {
         _playQueueMutex.WaitOne();
         try
         {
            _playQueue.Add( trackData );
         }
         finally
         {
            _playQueueMutex.ReleaseMutex();
         }
      }

      ///
      /// Remove a track from the head of the playlist most recently
      /// played.
      ///
      /// \note Updates the 
      PlayableData _PlaylistPop()
      {
         _playQueueMutex.WaitOne();
         try
         {
            // At this time I see no reason to support popping the current
            // playing track. :)
            if (0 >=_playQueueCurrentTrack)
               throw new ApplicationException( "No played tracks to pop" );

            if (_playQueue.Count > 0)
            {
               PlayableData returnData = (PlayableData)_playQueue[0];
               _playQueue.RemoveAt(0);

               // Fix up current track pointer 
               -- _playQueueCurrentTrack;

               return returnData;
            }

            return null;        // list empty.
         }
         finally
         {
            _playQueueMutex.ReleaseMutex();
         }
      }

      ///
      /// just returns the size of the play queue
      ///
      int _PlaylistGetCount()
      {
         _playQueueMutex.WaitOne();
         try
         {
            return _playQueue.Count;
         }
         finally
         {
            _playQueueMutex.ReleaseMutex();
         }
      }

      ///
      /// This is sort of random. We sometimes play tracks even if
      /// they suck.
      ///
      /// \return true if this track doesn't suck too much
      ///
      bool _WantToPlayTrack( PlayableData info )
      {
         if (null == _controllingUser) // Nobody exists, just play it.
            return true;

         uint trackKey = info.key;

         // Now, decide whether we are going to actually PLAY this
         // track. 

         uint suck = _database.GetSuck( _controllingUser.id,
                                        trackKey );

         uint suckThresh = (uint)_rng.Next( 01000, 09000 );
         if (suck > suckThresh)
         {
            _Trace( " Rejected. suck:" + suck
                    + " suckThresh:" + suckThresh );

            info.evaluation = TrackEvaluation.SUCK_TOO_MUCH;
            return false;       // methinks it sucketh too much
         }

         // If no controlling mood exists, we're happy with this track
         if (null == _controllingMood)
            return true;

         uint mood = _database.GetAppropriate( _controllingUser.id,
                                               _controllingMood.id,
                                               trackKey );

         uint moodThresh = (uint)_rng.Next( 01000, 09000 );
         if (mood < moodThresh)
         {
            _Trace( " Rejected. mood:" + mood
                    + " moodThresh:" + moodThresh );

            info.evaluation = TrackEvaluation.WRONG_MOOD;
            return false;       // not in the mood
         }

         info.evaluation = TrackEvaluation.ALL_GOOD;
         return true;           // good enough
      }
         


      ///
      /// \return true if the file pointed to by fullPath is already
      ///   in the database, false otherwise
      ///
      public bool EntryExists( string fullPath )
      {
         // _Trace( "[EntryExists]" );
         lock (_serializer)
         {
            return _database.Mp3FileEntryExists( fullPath );
         }
      }

      ///
      /// Adds the info for this local track-on-disk to the database
      ///
      public void Add( PlayableData newData )
      {
         lock (_serializer)
         {
            _database.Add( newData );
         }
      }

      ///
      /// A function that tells the database this file seems to exist
      /// on the disk (useful for refinding files that disappeared
      /// temporarily, say during a network outage).
      ///
      public void FileIsNotMissing( string fullPath )
      {
         _Trace( "[FileIsNotMissing] " + fullPath );
         lock (_serializer)
         {
            _database.TrackIsNotMissing( fullPath,
                                         StatusDatabase.TrackStatus.OK );
         }
      }

      ///
      /// Stop!
      ///
      public void StopPlaying()
      {
         lock (_serializer)
         {
            _shouldBePlaying = false;
            _player.Stop();
            ++ _changeCount;
         }
      }

      ///
      /// Starts playback if stopped
      ///
      public void StartPlaying()
      {
         lock (_serializer)
         {
            PlayableData file = _PlaylistGetCurrent();
            if (null != file)
            {
               _player.PlayFile( file.filePath, file.key );
               _shouldBePlaying = true;
            }
            ++ _changeCount;
         }
      }

      ///
      /// Return the list of available moods (as Mood)
      ///
      public Mood [] GetMoodList( Credentials cred )
      {
         lock (_serializer)
         {
            ArrayList moodList = _database.GetMoodList( cred );
            return (Mood[])moodList.ToArray(typeof(Mood));
         }
      }

      public Credentials [] GetUserList()
      {
         lock (_serializer)
         {
            ArrayList credList = _database.GetUserList();
            return (Credentials[])credList.ToArray(typeof(Credentials));
         }
      }

      public Credentials CreateUser( string name )
      {
         lock (_serializer)
         {
            _database.CreateUser( name );
            Credentials cred;
            if (!_database.GetUser( name, out cred ))
            {
               throw new ApplicationException( 
                  "Internal error, could not retrieve just-created user" );
            }

            return cred;
         }
      }

      ///
      /// Create and return a new mood for this user
      ///
      public Mood CreateMood( Credentials cred, string name )
      {
         lock (_serializer)
         {
            _database.CreateMood( cred, name );
            Mood mood;

            if (!_database.GetMood( cred, name, out mood ))
            {
               throw new ApplicationException( 
                  "Internal error, could not retrieve just-created mood" );
            }
            return mood;
         }
      }

      public void RenewLogon( Credentials cred )
      {
         lock (_serializer)
         {
            if (null == cred)
            {
               throw new ApplicationException( "no credentials" );
            }

            // TODO: see if this user is in the database.
            ++_changeCount;
            _controllingUser = (Credentials)cred;

            _database.StoreController( _controllingUser, new Mood() );
         }
      }

      public void SetMood( Credentials cred, Mood mood )
      {
         lock (_serializer)
         {
            if (null == cred)
               throw new ArgumentException( "may not be null", "cred" );

            if (_controllingUser.id != cred.id)
               throw new ApplicationException( "You are not in control" );

            ++_changeCount;
            _controllingMood = GetMood( mood.id );

            _database.StoreController( _controllingUser, 
                                       _controllingMood );
         }
      }

      ///
      /// Get an existing mood by name for a given user
      ///
      public Mood GetMood( Credentials cred, 
                           string name )
      {
         lock (_serializer)
         {
            if (null == cred)
               return null;

            Mood mood;
            if (!_database.GetMood( cred, name, out mood ))
               return null;

            return mood;
         }
      }

      ///
      /// get the mood data, given that you know its id:
      ///
      public Mood GetMood( uint id )
      {
         Mood mood;
         if (!_database.GetMood( id, out mood ))
            return null;
         
         return mood;
      }

      ///
      /// Get the credentials for this user
      ///
      public Credentials GetUser( string name )
      {
         lock (_serializer)
         {
            Credentials cred;
            if (!_database.GetUser( name, out cred ))
               return null;

            return cred;
         }
      }

      public Credentials GetUser( uint uid )
      {
         lock (_serializer)
         {
            Credentials cred;
            if (!_database.GetUser( uid, out cred ))
               return null;

            return cred;
         }
      }



      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "Backend" );
      }

      ///
      /// The Mp3 Playback Engine
      ///
      SimpleMp3Player.Player _player;

      ///
      /// Desired number of tracks in the play queue. (Static config)
      ///
      int _desiredQueueSize = 3;

      string _connectionString;

      ///
      /// A few past tracks, the current track, and all queued future
      /// tracks. (as PlayableData for now I guess)
      ///
      ArrayList _playQueue = new ArrayList();

      ///
      /// Offset of the curent playing track in _playQueue
      ///
      int       _playQueueCurrentTrack = -1; // haven't played track 0 yet

      ///
      /// Mutex to protect _playQueue and _playQueueCurrentTrack, etc
      /// from various threads.
      ///
      Mutex     _playQueueMutex = new Mutex();

      ///
      /// This is incremented every time we go forward, and decremented
      /// every time we go back.
      ///
      /// \bug
      ///   If you leave hte player going for a VERY long time, this will
      ///   overflow.
      ///
      long      _trackCounter = 0;

      ///
      /// This is simply a hash value indicating whether the interface
      /// should be updated on the client side.
      ///
      long      _changeCount = 0;

      ///
      /// If the user presses stop, this becomes false. Etc. The
      /// back end may stop playing if its queue runs out--this is 
      /// separate so we know whether to restart it.
      ///
      bool      _shouldBePlaying = true;

      ///
      /// This one protects us from multithreaded access due to remoting.
      /// (This is a MarshalByRefObject.) Lock on the _serializer, not the
      /// "this" reference.
      ///
      object    _serializer = new object();
      // Mutex     _serializer = new Mutex();

      ///
      /// How many finished-playing tracks to keep in the queue
      ///
      uint      _maxFinishedPlaying = 20;

      ///
      /// Credentials of the current user 
      ///
      /// \todo add multiuser support? maybe?
      ///
      Credentials _controllingUser = new Credentials();

      ///
      /// Controlling user's mood
      ///
      /// \todo Support multiple moods per-user. I think.
      ///
      Mood _controllingMood = new Mood();

      Random _rng = new MyRandom(); // Random numbers are cool

      ///
      /// Singleton object -- the only backend object that should exist
      ///
      static Backend _theBackend = null;

      //
      // References to friendly objects we know and love
      //

      StatusDatabase _database;

      ///
      /// The compressor system. This derives from BOTH IAudioProcessor
      /// and IMultiBandCompressor. It is cast to both. Isn't this fun?
      /// No? Well, OK.
      ///
      IAudioProcessor _compressor;

      ///
      /// A mutex to prevent audio from being mangled when settings
      /// are updated. (So, it prevents any processor from working while
      /// its settings are being changed, thus preventing them from
      /// having to worry about concurrency.)
      ///
      Mutex _audioMutex = new Mutex();

      ///
      /// The denormalization fix. Talk to your doctor or pharmacist.
      ///
      double _denormalFix = Denormal.denormalFixValue;

      Quality _qualityLevel = Quality.HIGH;

      Backend.CompressionType _compressionType = CompressionType.MULTIBAND;
   }
} // tam namespace


#if QQQ // comment out

      ///
      /// Calculate average power of this buffer, and update any 
      /// decaying means or whatever.
      ///
      void _UpdateAveragePower( byte [] buffer, 
                                int length,
                                bool compress )
      {
         Debug.Assert( length % 4 == 0, 
                       "I could have sworn this was 16 bit stereo" );

         if (length < 4)
            return;

         /// 
         /// \bug Should not be hardcoded 16-bit stereo
         ///

         // Assumes the buffer is 16-bit little-endian stereo. Ew!

         long sum = 0;
         int offset = 0;
         while (offset + 4 < length)
         {
            // Heck - truncate to 16-bit audio
            long sample = (((long)(sbyte)buffer[offset + 1] << 8) |
                           ((long)buffer[offset] ));
            sum += sample * sample;

            sample = (((long)(sbyte)buffer[offset + 3] << 8) |
                      ((long)buffer[offset + 2] ));
            sum += sample * sample;

            offset += 4;
         }

         // Root mean square. I like an acronym that actually helps.
         // Note this is the rms / (2^8). :)
         // double rms = Math.Sqrt( sum / (length / 2) );

         // TODO: add this value in to the average for this track.
         // TODO: store the rms value on a per-track basis for later
         //       levelling efforts
         // TODO: we don't really need to calculate the rms value of each
         //       buffer, do we? I think we're supposed to save the 
         //       square and count, then average and sqrt when the track
         //       ends!
      }

#endif // QQQ
