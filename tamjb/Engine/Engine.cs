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
   using System.Runtime.Remoting.Lifetime; // for ILease
   using System.Threading;

   using byteheaven.tamjb.Interfaces;
   using byteheaven.tamjb.SimpleMp3Player;

#if USE_POSTGRESQL
   using Npgsql;
#endif

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
         _Trace( "[Engine]" );

         try
         {
            if (null == _connectionString)
               throw new ApplicationException( "Engine is not properly initialized by the server" );

            _database = new StatusDatabase( _connectionString );

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

            _controllingUser = GetDefaultCredentials();
            _controllingMood = GetDefaultMood();
         }
         catch (Npgsql.NpgsqlException ne)
         {
            throw new ApplicationException( "NgpsqlException" +
                                            ne.ToString() );
         }
      }
      
      ~Engine()
      {
         _Trace( "[~Engine]" );
         _database = null;

      }

      //
      // foo
      //
      public ICredentials GetDefaultCredentials()
      {
         _Lock();
         try
         {
            // Return built-in user mr. guest
            Credentials cred;
            _database.GetUser( "guest", out cred );
            return cred;
         }
         finally
         {
            _Unlock();
         }
      }

      ///
      /// The default mood. When you don't know how you feel!
      ///
      public IMood GetDefaultMood()
      {
         _Lock();
         try
         {
            Mood mood;
            _database.GetMood( "unknown", out mood );
            return mood;
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
                                     _changeCount );
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
      public void IncreaseSuckZenoStyle( ICredentials cred,
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

      public void DecreaseSuckZenoStyle( ICredentials cred,
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

      public void IncreaseAppropriateZenoStyle( ICredentials cred,
                                                IMood mood,
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

      public void DecreaseAppropriateZenoStyle( ICredentials cred,
                                                IMood mood,
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
      public void GetAttributes( ICredentials cred,
                                 IMood mood,
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
      public void GotoNextFile( ICredentials cred, uint currentTrackKey )
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

      public void GotoPrevFile( ICredentials cred, uint currentTrackKey )
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
            correction *= RATIO;

            // Write new values to the samples: left

            long sample = (long)_SoftClip( left * correction );
            buffer[offset]     = (byte)(sample & 0xff);
            buffer[offset + 1] = (byte)(sample >> 8);

            // Now the right!

            sample = (long)_SoftClip( right * correction );
            buffer[offset + 2] = (byte)(sample & 0xff);
            buffer[offset + 3] = (byte)(sample >> 8);

            offset += 4;
         }

//          ++ _spew;
//          if (_spew > 25)
//          {
//             _spew = 0;
//             _Trace( "POWER: " + _decayingAveragePower
//                     + ", SCALE: " + correction);
//          }
      }

      double _SoftClip( double original )
      {
         if (original > CLIP_THRESHOLD) // Soft-clip 
         {
//             if (original > 32767 || original < -32767)
//             {
//                // Fascinating, soft clipping saved us from an awful noise!
//                Console.WriteLine( "CLIP: " + original );
//             }

            if (original > 0)
            {
               // Unsophisticated asympotic clipping algorithm
               // I came up with in the living room in about 15 minutes.
               return (CLIP_THRESHOLD +
                       (CLIP_LEFTOVER * (1 - (CLIP_THRESHOLD / original)))); 
            }
            else
            {
               // Unsophisticated asympotic clipping algorithm
               // I came up with in the living room in about 15 minutes.
               return -(CLIP_THRESHOLD -
                        (CLIP_LEFTOVER * (1 + (CLIP_THRESHOLD / original)))); 
            }
         }

         return original;
      }

      ///
      /// Target power level for compression/expansion.
      /// 
      /// Hot-mastered albums have an average power level with
      /// like -3dB from the absolute max level. Oh well, might 
      /// as well match that.
      ///
      static readonly double TARGET_POWER_LEVEL = 16000.0;

      ///
      /// Level below which we stop compressing and start
      /// expanding (if possible)
      ///
      static readonly double GATE_LEVEL = 1000.0;

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
      static readonly double RATIO = 0.833;

      //
      // Attack time really should be more than a 10 milliseconds to 
      // avoid distortion on kick drums, unless the release time is 
      // really long and you want to use it as a limiter, etc.
      //
      static readonly double ATTACK_RATIO_NEW = 0.002;
      static readonly double ATTACK_RATIO_OLD = 0.998;

      static readonly double DECAY_RATIO_NEW = 0.00000035;
      static readonly double DECAY_RATIO_OLD = 0.99999965;

      // Sample value for start of soft clipping. Leftover must
      // be 32767 - CLIP_THRESHOLD.
      static readonly double CLIP_THRESHOLD = 16000.0;
      static readonly double CLIP_LEFTOVER =  16767.0;

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

//          ++ _spew;
//          if (_spew > 10)
//          {
//             _spew = 0;
//             _Trace( "DECAY: " + _decayingAveragePower 
//                     + "RMS: " + rms );
//          }
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
      /// Returning null here guarantees that this object will
      /// stay in memory for the life of the server.
      /// 
      public override object InitializeLifetimeService()
      {
         return null; // never delete server

         // Shorten lease here for testing. This is for debugging
         // only, comment out in real code. :)
//          Console.WriteLine( "WARNING: shortening lease for testing" );
//          ILease lease = (ILease)base.InitializeLifetimeService();

//          if (lease.CurrentState == LeaseState.Initial)
//          {
//             lease.InitialLeaseTime = TimeSpan.FromSeconds(25);
//             lease.SponsorshipTimeout = TimeSpan.FromSeconds(26);
//             lease.RenewOnCallTime = TimeSpan.FromSeconds(0);
//          }

//          return lease;
      }

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "Engine" );
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
      ICredentials _controllingUser = null;

      ///
      /// Controlling user's mood
      ///
      /// \todo Support multiple moods per-user. I think.
      ///
      IMood _controllingMood = null;

      Random _rng = new Random(); // Random numbers are cool

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

      StatusDatabase _database;

      //
      // This is the average rms power of the current track decaying
      // exponentially over time. Normalized to 16 bits.
      //
      // Initially maxed out to avoid clipping
      //
      double _decayingAveragePower = (float)32767.0;

   }
} // tam namespace
