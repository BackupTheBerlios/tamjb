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

#if USE_POSTGRESQL
   using Npgsql;
#endif

   ///
   /// The main jukebox player object.
   ///
   public class Backend
   {
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
            return SimpleMp3Player.Player.bufferSize;
         }
         set
         {
            SimpleMp3Player.Player.bufferSize = value;
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
            return SimpleMp3Player.Player.buffersInQueue;
         }
         set
         {
            SimpleMp3Player.Player.buffersInQueue = value;
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
            return SimpleMp3Player.Player.buffersToPreload;
         }
         set
         {
            SimpleMp3Player.Player.buffersToPreload = value;;
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
                               string connectionString )
      {
         _theBackend = new Backend( desiredQueueSize, connectionString );
      }

      ///
      /// \warning You must set the (static) connectionString property
      ///   before any attempt to construct an engine.
      /// 
      protected Backend( int desiredQueueSize, 
                         string connectionString )
      {
         _Trace( "[Backend]" );

         _desiredQueueSize = desiredQueueSize;
         _connectionString = connectionString;

         try
         {
            if (null == _connectionString)
               throw new ApplicationException( "Engine is not properly initialized by the server" );

            _database = new StatusDatabase( _connectionString );

            // Todo: initialize compressor from stored settings in database
            _compressor = _LoadCompressor();

            // Get defaults from this kind of thing?
            // string bufferSize = 
            //    ConfigurationSettings.AppSettings ["BufferSize"];
                        
            // Set up default buffering for the audio engine
            SimpleMp3Player.Player.bufferSize = 44100 / 8 ;
            SimpleMp3Player.Player.buffersInQueue = 40;
            SimpleMp3Player.Player.buffersToPreload = 20;

            // Set up callbacks
            SimpleMp3Player.Player.OnTrackFinished +=
               new TrackFinishedHandler( _TrackFinishedCallback );

            SimpleMp3Player.Player.OnTrackPlayed +=
               new TrackStartingHandler( _TrackStartingCallback );
            
            SimpleMp3Player.Player.OnReadBuffer +=
               new ReadBufferHandler( _TrackReadCallback );

            _controllingUser = null;
            _controllingMood = null;
         }
         catch (Npgsql.NpgsqlException ne)
         {
            throw new ApplicationException( "NgpsqlException" +
                                            ne.ToString() );
         }
      }
      
      ~Backend()
      {
         _Trace( "[~Backend]" );
         // System.Runtime.Remoting.RemotingServices.Disconnect(this);
         _database = null;

      }

      //
      // Get the current mood. May return null for either or both
      //
      public void GetCurrentUserAndMood( ref Credentials cred,
                                         ref Mood mood )
      {
         _Lock();
         try
         {
            if (cred == null)
            {
               cred = _controllingUser;
            }
            else
            {
               cred.name = _controllingUser.name;
               cred.id = _controllingUser.id;
            }

            if (mood == null)
            {
               mood = _controllingMood;
            }
            else
            {
               mood.name = _controllingMood.name;
               mood.id = _controllingMood.id;
            }
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// \todo Does state get copied both ways in spite of this
      ///   code?
      ///
      public bool CheckState( ref EngineState state )
      {
         _Lock();
         try
         {
            if (null == state)
               throw new ArgumentException( "may not be null", "state" );

            // No change? nothing to do!
            if (((EngineState)state).changeCount == _changeCount)
               return false;

            state.isPlaying = _shouldBePlaying;
            state.currentTrackIndex = _playQueueCurrentTrack;
            state.playQueue = _playQueue; // reference to internal queue?
            state.changeCount = _changeCount;
//             state = new EngineState( _shouldBePlaying,
//                                      _playQueueCurrentTrack,
//                                      _playQueue,
//                                      _changeCount );
            return true;
         }
         finally
         {
            _Unlock();
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
         _Lock();
         try
         {
            // Enqueue at most one song each time this is polled
            if (unplayedTrackCount < _desiredQueueSize)
               EnqueueRandomSong();

            // If playback has stopped, restart it now.
            if (_shouldBePlaying && ( ! Player.isPlaying ))
            {
               _Trace( "Hey, we are not playing. Restarting.." );
               GotoNext();
            }
         }
         catch (Npgsql.NpgsqlException ne)
         {
            // NpgsqlException has a long-standing problem where it is
            // not marked serializable. :(
            throw new ApplicationException( "NpgsqlException" 
                                            + ne.ToString() );
         }
         catch (Exception e)
         {
            _Trace( "Exception propagating from Poll()" );
            _Trace( e.ToString() );
            throw;
         }
         finally
         {
            _Unlock();
         }
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

         // Pick a random suck and mood threshold
         int suckThresh = _rng.Next( 00500, 09500 );
         int moodThresh = _rng.Next( 00500, 09500 );
         
         // Pick a song and go.
         uint nextKey;
         uint count = _database.PickRandom( _controllingUser, 
                                            _controllingMood,
                                            suckThresh,
                                            moodThresh,
                                            out nextKey );

         if (0 == count)
         {
            _Trace( "  Playlist Empty: No songs found" );
            return false;    // No songs found
         }

         PlayableData next = _database.GetFileInfo( nextKey );
         _PlaylistAppend( next );

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

         _Lock();
         try
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
         finally
         {
            _Unlock();
         }
      }

      public void DecreaseSuckZenoStyle( Credentials cred,
                                         uint trackKey )
      {
         _Trace( "[DecreaseSuckZenoStyle]" );

         _Lock();
         try
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
         finally
         {
            _Unlock();
         }
      }

      public void IncreaseAppropriateZenoStyle( Credentials cred,
                                                Mood mood,
                                                uint trackKey )
      {
         _Trace( "[IncreaseAppropriateZenoStyle]" );

         _Lock();
         try
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
         finally
         {
            _Unlock();
         }
      }

      public void DecreaseAppropriateZenoStyle( Credentials cred,
                                                Mood mood,
                                                uint trackKey )
      {
         _Trace( "[DecreaseAppropriateZenoStyle]" );

         _Lock();
         try
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
         finally
         {
            _Unlock();
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
            _Lock();
            try
            {
               return (ITrackInfo)_PlaylistGetCurrent();
            }
            finally
            {
               _Unlock();
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
            _Lock();
            try
            {
               return _PlaylistGetCount() - _playQueueCurrentTrack;
            }
            finally
            {
               _Unlock();
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
            _Lock();
            try
            {
               return (ITrackInfo)_PlaylistGetAt( index );
            }
            finally
            {
               _Unlock();
            }
         }
      }

      public int Count
      {
         get
         {
            _Lock();
            try
            {
               return _PlaylistGetCount();
            }
            finally
            {
               _Unlock();
            }
         }
      }

      public ITrackInfo GetFileInfo( uint key )
      {
         _Lock();
         try
         {
            return (ITrackInfo)_database.GetFileInfo( key );
         }
         finally
         {
            _Unlock();
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
         _Lock();
         try
         {
            suck = _database.GetSuck( cred.id, trackKey );
            appropriate = _database.GetAppropriate( cred.id, 
                                                    mood.id, 
                                                    trackKey );
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// GotoFile function with credentials for future expansion.
      ///
      public void GotoNextFile( Credentials cred, uint currentTrackKey )
      {
         _Trace( "[GotoNextFile]" );

         _Lock();
         try
         {
            GotoNext();
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Start playing the next file in the queue
      ///
      void GotoNext()
      {
         _Trace( "[GotoNext]" );

         ++_changeCount;

         PlayableData nextFile = _PlaylistGoNext();
         if (null != nextFile)
         {
            _Trace( "  NEXT = " + nextFile.title );
            Player.PlayFile( nextFile.filePath, nextFile.key );
            _shouldBePlaying = true;
         }
      }

      public void GotoPrevFile( Credentials cred, uint currentTrackKey )
      {
         _Trace( "[GotoPrevFile]" );

         _Lock();
         try
         {
            GotoPrev();
         }
         finally
         {
            _Unlock();
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
            Player.PlayFile( prevFile.filePath, prevFile.key );
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
         ++ _changeCount;

         // Previous track could be nothing?
         if (null != info)
         {            
            Trace.WriteLine( "Track Finished " + info.key );

            // Because of the threading, I don't know we can guarantee
            // this will never happen. Let's find out:
            Debug.Assert( info.key == currentTrack.key,
                          "finished track != playing track" );
         }

         if (_shouldBePlaying)  // don't bother if we should be stopped
         {
            // Don't acquire the _serializer mutex here, because this could
            // cause a deadlock!

            // Advance the playlist (_Playlist* functions are threadsafe)
            PlayableData nextInfo = _PlaylistGoNext();
            if (null != nextInfo)
            {
               // Tell the player to play this track next
               Player.SetNextFile( nextInfo.filePath, nextInfo.key );
            }
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
            _compressor.Process( buffer, length );
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
            return _compressor.compressAttack;
         }
         set
         {
            _Lock();
            try
            {
               _audioMutex.WaitOne();
               try
               {
                  _compressor.compressAttack = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
            finally
            {
               _Unlock();
            }
         }
      }

      public double compressDecay
      {
         get
         {
            return _compressor.compressDecay;
         }
         set
         {
            _Lock();
            try
            {
               _audioMutex.WaitOne();
               try
               {
                  _compressor.compressDecay = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
            finally
            {
               _Unlock();
            }
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public int compressThreshold
      {
         get
         {
            return _compressor.compressThreshold;
         }
         set
         {
            _Lock();
            try
            {
               _audioMutex.WaitOne();
               try
               {
                  _compressor.compressThreshold = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
            finally
            {
               _Unlock();
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
            return _compressor.gateThreshold;
         }
         set
         {
            _Lock();
            try
            {
               _audioMutex.WaitOne();
               try
               {
                  _compressor.gateThreshold = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
            finally
            {
               _Unlock();
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
            return _compressor.compressRatio;
         }
         set
         {
            _Lock();
            try
            {
               _audioMutex.WaitOne();
               try
               {
                  _compressor.compressRatio = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
            finally
            {
               _Unlock();
            }
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public int clipThreshold
      {
         get
         {
            return _compressor.clipThreshold;
         }
         set
         {
            _Lock();
            try
            {
               _audioMutex.WaitOne();
               try
               {
                  _compressor.clipThreshold = value;
               }
               finally
               {
                  _audioMutex.ReleaseMutex();
               }

               _StoreCompressSettings();
            }
            finally
            {
               _Unlock();
            }
         }
      }

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
         double rms = Math.Sqrt( sum / (length / 2) );

         // TODO: add this value in to the average for this track.
         // TODO: store the rms value on a per-track basis for later
         //       levelling efforts
      }

      ///
      /// Save the current compression settings
      ///
      void _StoreCompressSettings()
      {
         Debug.Assert( null != _compressor );

         // Create a serializer, and serialize to rom
         XmlSerializer serializer = 
            new XmlSerializer( typeof(Compressor) );
         
         StringWriter str = new StringWriter();
         serializer.Serialize( str, _compressor );
         
         _database.StoreCompressSettings( str.ToString() );
      }

      ///
      /// load a compressor using the settings in the database, or a default
      /// one if the settings are not found/valid
      ///
      Compressor _LoadCompressor()
      {
         Compressor compressor = null;
         try
         {
            string settings = _database.GetCompressSettings();
            if (null != settings)
            {
               XmlSerializer serializer = 
                  new XmlSerializer( typeof( Compressor ) );

               StringReader str = new StringReader( settings );
               compressor = (Compressor)serializer.Deserialize( str );
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }

         if (null == compressor)
         {
            _Trace( "Note: compression settings not found, using defaults" );
            compressor = new Compressor(); // load with defaults
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
            if (_playQueueCurrentTrack <= 0)
               return null;

            -- _playQueueCurrentTrack;
            -- _trackCounter;
            return (PlayableData)_playQueue[_playQueueCurrentTrack];
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
      /// \return true if the file pointed to by fullPath is already
      ///   in the database, false otherwise
      ///
      public bool EntryExists( string fullPath )
      {
         // _Trace( "[EntryExists]" );
         _Lock();
         try
         {
            return _database.Mp3FileEntryExists( fullPath );
         }
         catch (Npgsql.NpgsqlException ne) 
         {
            // These damn things don't Reflect properly
            throw new ApplicationException( ne.ToString() );
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Adds the info for this local track-on-disk to the database
      ///
      public void Add( PlayableData newData )
      {
         _Lock();
         try
         {
            _database.Add( newData );
         }
         catch (Npgsql.NpgsqlException ne) // These damn things don't Reflect properly
         {
            throw new ApplicationException( ne.ToString() );
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Stop!
      ///
      public void StopPlaying()
      {
         _Lock();
         try
         {
            _shouldBePlaying = false;
            Player.Stop();
            ++ _changeCount;
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Starts playback if stopped
      ///
      public void StartPlaying()
      {
         _Lock();
         try
         {
            PlayableData file = _PlaylistGetCurrent();
            if (null != file)
            {
               Player.PlayFile( file.filePath, file.key );
               _shouldBePlaying = true;
            }
            ++ _changeCount;
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Return the list of available moods (as Mood)
      ///
      public Mood [] GetMoodList( Credentials cred )
      {
         _Lock();
         try
         {
            ArrayList moodList = _database.GetMoodList( cred );
            return (Mood[])moodList.ToArray(typeof(Mood));
         }
         finally
         {
            _Unlock();
         }
      }

      public Credentials [] GetUserList()
      {
         _Lock();
         try
         {
            ArrayList credList = _database.GetUserList();
            return (Credentials[])credList.ToArray(typeof(Credentials));
         }
         finally
         {
            _Unlock();
         }
      }

      public Credentials CreateUser( string name )
      {
         _Lock();
         try
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
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Create and return a new mood for this user
      ///
      public Mood CreateMood( Credentials cred, string name )
      {
         _Lock();
         try
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
         finally
         {
            _Unlock();
         }
      }

      public void RenewLogon( Credentials cred )
      {
         _Lock();
         try
         {
            if (null == cred)
            {
               throw new ApplicationException( "no credentials" );
            }

            // TODO: see if this user is in the database.
            ++_changeCount;
            _controllingUser = (Credentials)cred;
         }
         finally
         {
            _Unlock();
         }
      }

      public void SetMood( Credentials cred, Mood mood )
      {
         _Lock();
         try
         {
            if (null == cred || null == mood)
               throw new ApplicationException( "bad parameter" );

            if (_controllingUser.id != cred.id)
               throw new ApplicationException( "You are not in control" );

            // TODO: see if this user/mood is in the database

            ++_changeCount;
            _controllingMood = (Mood)mood;
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Get an existing mood by name for a given user
      ///
      public Mood GetMood( Credentials cred, string name )
      {
         _Lock();
         try
         {
            if (null == cred)
               return null;

            Mood mood;
            if (!_database.GetMood( cred, name, out mood ))
               return null;

            return mood;
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Get the credentials for this user
      ///
      public Credentials GetUser( string name )
      {
         _Lock();
         try
         {
            Credentials cred;
            if (!_database.GetUser( name, out cred ))
               return null;

            return cred;
         }
         finally
         {
            _Unlock();
         }
      }


      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "Backend" );
      }

      ///
      /// Wrapper function to handle serialization of remote controls.
      ///
      void _Lock()
      {
         if (false == _serializer.WaitOne( 3000,
                                           false ))
         {
            throw new ApplicationException( 
               "_serializer Timed out waiting for lock" );
         }
      }

      void _Unlock()
      {
         _serializer.ReleaseMutex();
      }

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
      /// This one protects us from multithreaded access due to remoting,
      /// as this is a MarshalByRefObject. Actually I think we need a slightly
      /// better interface than what I originally threw together here...this
      /// doesn't scale well. :)
      ///
      Mutex     _serializer = new Mutex();

      ///
      /// How many finished-playing tracks to keep in the queue
      ///
      uint      _maxFinishedPlaying = 20;

      ///
      /// Credentials of the current user 
      ///
      /// \todo add multiuser support? maybe?
      ///
      Credentials _controllingUser = null;

      ///
      /// Controlling user's mood
      ///
      /// \todo Support multiple moods per-user. I think.
      ///
      Mood _controllingMood = null;

      Random _rng = new Random(); // Random numbers are cool

      ///
      /// Singleton object -- the only backend object that should exist
      ///
      static Backend _theBackend = null;

      //
      // References to friendly objects we know and love
      //

      StatusDatabase _database;

      ///
      /// Our main audio processor: the level manager
      ///
      Compressor _compressor;

      ///
      /// A mutex to prevent audio from being mangled when settings
      /// are updated. (So, it prevents any processor from working while
      /// its settings are being changed, thus preventing them from
      /// having to worry about concurrency.)
      ///
      Mutex _audioMutex = new Mutex();
   }
} // tam namespace
