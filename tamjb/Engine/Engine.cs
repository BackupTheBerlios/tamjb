/// \file
/// $Id$
///
/// The jukebox player engine.
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
   using System.Runtime.Remoting;
   using System.Threading;

   using byteheaven.tamjb.Interfaces;
   using byteheaven.tamjb.SimpleMp3Player;

   /// 
   /// Attribute tables in the database
   ///
   enum Tables : uint
   {
      // Suck database is always attribute 0 because it's special!
      // Note that it's really a DOESNTSUCK table, because higher values
      // always mean more-desirable.
      DOESNTSUCK = 0,              
   }

   ///
   /// The main jukebox player object.
   ///
   public class Engine : MarshalByRefObject, IEngine
   {
      ///
      /// get/set the database connection string (as an url, must
      /// be file:something for SQLite.
      ///
      static public string connectionString
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
      static public int desiredQueueSize
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
      /// Constructor with no parameters is required for remoting under
      /// mono. Right now.
      ///
      /// \warning You must set the (static) connectionString property
      ///   before any attempt to construct an engine.
      /// 
      public Engine( )
      {
         _Trace( "Engine" );

         if (null == _connectionString)
            throw new ApplicationException( "Engine is not properly initialized by the server" );

         _database = new StatusDatabase( _connectionString );

         /// \todo save the current state, or have a default set of
         ///   playlist criteria

         // By default, use the suck/wrong metrics and nothing else.
         _criteria = new PlaylistCriteria();
         _fileSelector = new FileSelector( _database, _criteria );

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
      }
      
      ~Engine()
      {
         _Trace( "~Engine" );
         _fileSelector = null;
         _database = null;
      }

      ///
      /// \todo Does state get copied both ways in spite of this
      ///   code?
      ///
      public bool CheckState( ref IEngineState state )
      {
         _Lock();
         try
         {
            if (state != null)
            {
               // No change? nothing to do!
               if (((EngineState)state).changeCount == _changeCount)
                  return false;
            }

            state = new EngineState( _shouldBePlaying,
                                     _playQueueCurrentTrack,
                                     _playQueue,
                                     _trackCounter,
                                     _changeCount,
                                     activeCriteria );
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
         // _Trace( "Poll" );
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
               GotoNextFile();
            }
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
         _Trace( "EnqueueRandomSong" );

         try
         {
            // Pick a song and go.
            PlayableData next = _fileSelector.ChooseNextSong();
            _PlaylistAppend( next );

            ++_changeCount;
            return true;
         }
         catch (PlaylistEmptyException e)
         {
            _Trace( "No songs found" );
            return false;
         }
      }

      ///
      /// Increases an attribute by 50% of the difference between its
      /// current level and 100%. Thus it theoretically never reaches
      /// 100% (cause integer math rounds down).
      ///
      public void IncreaseAttributeZenoStyle( uint attributeKey,
                                              uint trackKey )
      {
         _Trace( "IncreaseAttributeZenoStyle" );

         _Lock();
         try
         {
            ++_changeCount;

            uint level = _database.GetAttribute( attributeKey, trackKey );

            // The database shouldn't contain invalid attributes, right?
            Debug.Assert( level <= 10000 && level >= 0 );

            // how much is halfway from the current level to 100%?
            // (so: if it was 10%, it's now 10 + 90/2 = 55%.)

            uint difference = 10000 - level;
            if (difference > 0)    // avoid the tragic div/zero error.
               level += ( difference / 2 ); 

            // Is our math correct?
            Debug.Assert( level <= 10000 && level >= 0 );

            _database.SetAttribute( attributeKey, trackKey, level );
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Decreases an attribute. 
      ///
      /// \see _IncreaseAttributeZenoStyle
      ///
      public void DecreaseAttributeZenoStyle( uint attributeKey,
                                              uint trackKey )
      {
         _Trace( "DecreaseAttributeZenoStyle" );

         _Lock();

         try
         {
            ++_changeCount;

            uint level = _database.GetAttribute( attributeKey, trackKey );

            // The database shouldn't contain invalid attributes, right?
            Debug.Assert( level <= 10000 && level >= 0 );

            // how much is halfway from the current level to 100%?
            // (so: if it was 10%, it's now 10 + 90/2 = 55%.)

            level /= 2;

            _database.SetAttribute( attributeKey, trackKey, level );
         }
         finally
         {
            _Unlock();
         }
      }

      public void SetAttribute( uint attributeKey,
                                uint trackKey,
                                uint level )
      {
         _Lock();
         try
         {
            ++_changeCount;
            _database.SetAttribute( attributeKey, trackKey, level );
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

      public uint GetAttribute( uint playlistKey,
                                uint trackKey )
      {
         _Lock();
         try
         {
            return _database.GetAttribute( playlistKey, trackKey );
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Retrieve a criterion based on index. This is a reference
      /// to the live criterion being used to retrieve data, and changes
      /// to it are immediate. 
      ///
      public IPlaylistCriterion GetCriterion( uint index )
      {
         _Trace( "GetCriterion" );

         _Lock();
         try
         {
            // Return from local cache of all criteria.
            return (IPlaylistCriterion)_criterion[index];
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Activate a criterion
      ///
      /// \todo I hate this interface
      ///
      public void ActivateCriterion( uint index )
      {
         _Trace( "[ActivateCriterion] " + index );

         _Lock();
         try
         {
            // if playlist criterion is already active, do nothing

            if (null != _criteria.Find( index ))
               return;

            // Make sure the index is up to date
            _database.ImportNewFiles( index, 8000 );
            _criteria.Add( _criterion[index] );
            ++_changeCount;
         }
         finally
         {
            _Unlock();
         }
      }

      public void DeactivateCriterion( uint index )
      {
         _Trace( "[DeactivateCriterion] " + index );

         _Lock();
         try
         {
            // if playlist criterion is already active, do nothing
            _criteria.Remove( index );
            ++_changeCount;
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// Get the currently active criteria indexes.
      /// ome
      uint [] activeCriteria
      { 
         get
         {
            _Lock();
            try
            {
               // Build a list of the current criteria's keys.
               // This is kind of lame. 

               // I want a way of
               // authenticating users so that we can have per-user
               // "Suck" values.

               uint [] list = new uint[_criteria.Count];
               int i = 0;
               foreach (DictionaryEntry de in _criteria)
               {
                  PlaylistCriterion criterion = (PlaylistCriterion)de.Value;
                  list[i] = criterion.attribKey;
               }

               return list;
            }
            finally
            {
               _Unlock();
            }
         }
      }

//       void SetPlaylist( uint index )
//       {
//          _Trace( "SetPlaylist( " + index + ")" );

//          // Import any new entries from the local-files tables to the
//          // attribute tables. This is only necessary if some external
//          // program has twiddled our database while we were out. 

//          // Is this the right place for this? I know 50% is the right
//          // default value...for now.

//          // Always use the suck metric
//          _database.ImportNewFiles( (uint)Tables.DOESNTSUCK, 8000 );
//          _criteria.Add( _criterion[0] );

//          if (index != 0)
//          {
//             _database.ImportNewFiles( index, 8000 );
//             _criteria.Add( _criterion[index] );
//          }

//          _fileSelector.SetCriteria( _criteria );
//          ++_changeCount;
//       }

      ///
      /// Start playing the next file in the queue
      ///
      public void GotoNextFile()
      {
         _Trace( "[GotoNextFile]" );

         _Lock();
         try
         {
            PlayableData nextFile = _PlaylistGoNext();
            if (null != nextFile)
            {
               Player.PlayFile( nextFile.filePath, nextFile.key );
               _shouldBePlaying = true;
            }

            ++_changeCount;
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
      public void GotoPrevFile()
      {
         _Trace( "GotoPrevFile" );

         _Lock();
         try
         {
            PlayableData prevFile = _PlaylistGoPrev();
            _Trace( "PREV = " + prevFile.title );
            if (null != prevFile)
            {
               Player.PlayFile( prevFile.filePath, prevFile.key );
               _shouldBePlaying = true;
            }

            ++_changeCount;
         }
         finally
         {
            _Unlock();
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
         _Compress( buffer, length );
      }

      ///
      /// Exponential-decay-styled compressor.
      ///
      void _Compress( byte [] buffer, 
                      int length )
      {
         Debug.Assert( length % 4 == 0, 
                       "I could have sworn this was 16 bit stereo" );

         if (length < 4)
            return;

         /// 
         /// \bug Should not be hardcoded 16-bit stereo
         ///

         // Assumes the buffer is 16-bit little-endian stereo.

         int offset = 0;
         double correction;
         while (offset + 3 < length)
         {
            double magnitude;

            // Approximate the average power of both channels by summing
            // them (yes, phase differences are a problem but whatever)
            // And exponentially decay or release the level:

            double left = (((long)(sbyte)buffer[offset + 1] << 8) |
                           ((long)buffer[offset] ));

            // Fast attack, slow decay
            magnitude = Math.Abs(left);
            if (magnitude > _decayingAveragePower)
            {
               _decayingAveragePower = 
                  ((_decayingAveragePower * ATTACK_RATIO_OLD) +
                   (magnitude * ATTACK_RATIO_NEW));
            }
            else
            {
               _decayingAveragePower = 
                  ((_decayingAveragePower * DECAY_RATIO_OLD) +
                   (magnitude * DECAY_RATIO_NEW));
            }

            double right = (((long)(sbyte)buffer[offset + 3] << 8) |
                            ((long)buffer[offset + 2] ));

            magnitude = Math.Abs(right);
            if (magnitude > _decayingAveragePower)
            {
               _decayingAveragePower = 
                  ((_decayingAveragePower * ATTACK_RATIO_OLD) +
                   (magnitude * ATTACK_RATIO_NEW));
            }
            else
            {
               _decayingAveragePower = 
                  ((_decayingAveragePower * DECAY_RATIO_OLD) +
                   (magnitude * DECAY_RATIO_NEW));
            }

            // How far off from the target power are we?
            if (_decayingAveragePower < GATE_LEVEL)
               correction = TARGET_POWER_LEVEL / GATE_LEVEL;
            else
               correction = TARGET_POWER_LEVEL / _decayingAveragePower;

            // For inf:1 compression, use "offset", otherwise
            // use this ratio to get other ratios:
            // correction *= RATIO;

            // Compress the values

            long sample;
            sample = (long)(left * correction);

            buffer[offset]     = (byte)(sample & 0xff);
            buffer[offset + 1] = (byte)(sample >> 8);

            sample = (long)(right * correction);

            buffer[offset + 2] = (byte)(sample & 0xff);
            buffer[offset + 3] = (byte)(sample >> 8);

            offset += 4;
         }

         ++ _spew;
         if (_spew > 10)
         {
            _spew = 0;
            _Trace( "POWER: " + _decayingAveragePower
                    + ", SCALE: " + correction);
         }
      }

      ///
      /// Target power level for compression/expansion.
      /// 
      /// Something approximating 9dB of headroom
      ///
      static readonly double TARGET_POWER_LEVEL = 7000.0;

      ///
      /// Level below which we stop compressing and start
      /// expanding (if possible)
      ///
      static readonly double GATE_LEVEL = 150.0;

      /// 
      /// Compression ratio where for n:1 compression, 
      /// RATIO = (1 - 1/n).  or something.
      ///
      /// 1.0 = infinity:1
      /// 0.875 = 8:1
      /// 0.833 = 6:1
      /// 0.75 = 4:1
      /// 0.5 = 2:1
      /// 0.0 = no compression 
      ///
      static readonly double RATIO = 0.875;

      //
      // Attack time really should be more than a 10 milliseconds to 
      // avoid distortion on kick drums, unless the release time is 
      // really long and you want to use it as a limiter, etc.
      //
      static readonly double ATTACK_RATIO_NEW = 0.0004;
      static readonly double ATTACK_RATIO_OLD = 0.9996;

      static readonly double DECAY_RATIO_NEW = 0.000001;
      static readonly double DECAY_RATIO_OLD = 0.999999;

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

         ///
         /// \bug
         /// The decay rate will change with the sample rate and the
         /// buffer size, so this ratio REALLY shouldn't be hardcoded.
         ///
         _decayingAveragePower = 
            (_decayingAveragePower * 0.9) +
            (rms * 0.1);

         ++ _spew;
         if (_spew > 10)
         {
            _spew = 0;
            _Trace( "DECAY: " + _decayingAveragePower 
                    + "RMS: " + rms );
         }
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
         // _Trace( "EntryExists" );
         _Lock();
         try
         {
            return _database.Mp3FileEntryExists( fullPath );
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
      /// Returning null here guarantees that this object will
      /// stay in memory for the life of the server.
      /// 
      public override object InitializeLifetimeService()
      {
         return null;
      }

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "Engine" );
      }

      ///
      /// \todo This array is temporary. Create a real playlist 
      ///   info table or something. Indexed by Name, not uint.
      ///
      PlaylistCriterion [] _criterion = 
      {
         new PlaylistCriterion( (uint)Tables.DOESNTSUCK, 
                                8000,
                                6000 ),
         new PlaylistCriterion( 1, 8000, 6000 ),
         new PlaylistCriterion( 2, 8000, 6000 ),
         new PlaylistCriterion( 3, 8000, 6000 )
      };

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
      /// Desired number of tracks in the play queue. (Static config)
      ///
      static int _desiredQueueSize = 3;

      ///
      /// String used for accessing the deatabase (static config)
      ///
      /// Must be set by the server before any remote connections are
      /// accepted
      ///
      static string _connectionString = null;

      //
      // References to friendly objects we know and love
      //
      FileSelector   _fileSelector;

      // FileScanner    _fileScanner;

      StatusDatabase _database;

      PlaylistCriteria _criteria = new PlaylistCriteria();

      //
      // This is the average rms power of the current track decaying
      // exponentially over time. Normalized to 16 bits.
      //
      // Initially maxed out to avoid clipping
      //
      double _decayingAveragePower = (float)32767.0;

      long _spew = 0;

   }
} // tam namespace
