/// \file
/// $Id$
/// Player engine (non gui)

namespace tam
{
   using System;
   using System.Collections;
   using System.Data;
   using System.Diagnostics;
   using System.Runtime.Remoting;
   using System.Threading;
   using Mono.Data.SqliteClient;
   using tam.LocalFileDatabase;
   using tam.SimpleMp3Player;

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

   // [Serializable]
   // [NonSerialized]

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
      /// Constructor with no parameters is required for remoting under
      /// mono. Right now.
      ///
      /// \warning You must set the (static) connectionString property
      ///   before any attempt to construct an engine.
      /// 
      public Engine()
      {
         if (null == _connectionString)
            throw new ApplicationException( "Engine is not properly initialized by the server" );

         // Perhaps the one incredibly asinine thing about .NET: you have
         // to hardcode the type of database you are connecting to. Could
         // this be an intentional mistake? Hah.
         _databaseConnection = new SqliteConnection( _connectionString );
         _databaseConnection.Open();

         _database = new StatusDatabase( _databaseConnection );

         /// \todo save the current state, or have a default set of
         ///   playlist criteria

         // By default, use the suck/wrong metrics and nothing else.
         _criteria = new PlaylistCriteria();
         _fileSelector = new FileSelector( _database, _criteria );

         // Get defaults from this kind of thing?
         // string bufferSize = 
         //    ConfigurationSettings.AppSettings ["BufferSize"];
                        
         // Set up the audio player engine
         SimpleMp3Player.Player.bufferSize = 44100 / 4 ;
         SimpleMp3Player.Player.buffersInQueue = 16;
         SimpleMp3Player.Player.buffersToPreload = 16;

         // Set up callback to be called when a track finishes
         // playing:
//          SimpleMp3Player.Player.OnTrackFinished +=
//             new TrackFinishedHandler( _TrackFinishedCallback );

//          SimpleMp3Player.Player.OnTrackPlayed +=
//             new TrackStartingHandler( _TrackStartingCallback );
      }
      
      ~Engine()
      {
         Console.WriteLine( "[~Engine]" );
         _fileSelector = null;
         _database = null;

         if (null != _databaseConnection)
         {
            // Only after all other objects are done
            _databaseConnection.Close(); 
            _databaseConnection = null;
         }
      }

      ///
      /// \todo Does state get copied both ways in spite of this
      ///   code?
      ///
      public bool CheckState( ref IEngineState state )
      {
         _serializer.WaitOne();
         try
         {
            if (state != null)
            {
               // No change? nothing to do!
               if (((EngineState)state).changeCount == _changeCount)
                  return false;
            }

            state = new EngineState( Player.isPlaying,
                                     _playQueueCurrentTrack,
                                     _playQueue,
                                     _trackCounter,
                                     _changeCount );
            return true;
         }
         finally
         {
            _serializer.ReleaseMutex();
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
         try
         {
            _serializer.WaitOne();

            // Enqueue at most one song each time this is polled
            if (unplayedTrackCount < _desiredQueueSize)
               EnqueueRandomSong();

            // If playback has stopped, restart it now.
            if (! this.isPlaying)
               GotoNextFile();

            // Keep scanning for new files?
            // _fileScanner.CheckOneDirectory();
         }
         finally
         {
            _serializer.ReleaseMutex();
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
         Console.WriteLine( "EnqueueRandomSong" );

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
            Console.WriteLine( "No songs found" );
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
         try
         {
            _serializer.WaitOne();

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
            _serializer.ReleaseMutex();
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
         try
         {
            _serializer.WaitOne();

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
            _serializer.ReleaseMutex();
         }
      }

      public void SetAttribute( uint attributeKey,
                                uint trackKey,
                                uint level )
      {
         try
         {
            _serializer.WaitOne();
            _database.SetAttribute( attributeKey, trackKey, level );
         }
         finally
         {
            _serializer.ReleaseMutex();
         }
      }

      public bool isPlaying
      {
         get
         {
            return Player.isPlaying;
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
            try
            {
               _serializer.WaitOne();
               
               return (ITrackInfo)_PlaylistGetCurrent();
            }
            finally
            {
               _serializer.ReleaseMutex();
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
            try
            {
               _serializer.WaitOne();

               return _PlaylistGetCount() - _playQueueCurrentTrack;
            }
            finally
            {
               _serializer.ReleaseMutex();
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
            try
            {
               _serializer.WaitOne();

               return (ITrackInfo)_PlaylistGetAt( index );
            }
            finally
            {
               _serializer.ReleaseMutex();
            }
         }
      }

      public int Count
      {
         get
         {
            try
            {
               _serializer.WaitOne();

               return _PlaylistGetCount();
            }
            finally
            {
               _serializer.ReleaseMutex();
            }
         }
      }

      public ITrackInfo GetFileInfo( uint key )
      {
         try
         {
            _serializer.WaitOne();

            return (ITrackInfo)_database.GetFileInfo( key );
         }
         finally
         {
            _serializer.ReleaseMutex();
         }
      }

      public uint GetAttribute( uint playlistKey,
                                uint trackKey )
      {
         try
         {
            _serializer.WaitOne();

            return _database.GetAttribute( playlistKey, trackKey );
         }
         finally
         {
            _serializer.ReleaseMutex();
         }
      }

      ///
      /// Retrieve a criterion based on index. This is a reference
      /// to the live criterion being used to retrieve data, and changes
      /// to it are immediate. 
      ///
      public IPlaylistCriterion GetCriterion( uint index )
      {
         try
         {
            _serializer.WaitOne();

            // Return from local cache of all criteria.
            return (IPlaylistCriterion)_criterion[index];
         }
         finally
         {
            _serializer.ReleaseMutex();
         }
      }

      ///
      /// Activate a criterion
      ///
      /// \todo I hate this interface
      ///
      public void ActivateCriterion( uint index )
      {
         try
         {
            _serializer.WaitOne();

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
            _serializer.ReleaseMutex();
         }
      }

      public void DeactivateCriterion( uint index )
      {
         try
         {
            _serializer.WaitOne();

            // if playlist criterion is already active, do nothing
            _criteria.Remove( index );
            ++_changeCount;
         }
         finally
         {
            _serializer.ReleaseMutex();
         }
      }

      void SetPlaylist( uint index )
      {
         Console.WriteLine( "_SetPrimaryPlaylist( " + index + ")" );

         // Import any new entries from the local-files tables to the
         // attribute tables. This is only necessary if some external
         // program has twiddled our database while we were out. 

         // Is this the right place for this? I know 50% is the right
         // default value...for now.

         // Always use the suck metric
         _database.ImportNewFiles( (uint)Tables.DOESNTSUCK, 8000 );
         _criteria.Add( _criterion[0] );

         if (index != 0)
         {
            _database.ImportNewFiles( index, 8000 );
            _criteria.Add( _criterion[index] );
         }

         _fileSelector.SetCriteria( _criteria );
         ++_changeCount;
      }

      ///
      /// Start playing the next file in the queue
      ///
      public void GotoNextFile()
      {
         try
         {
            _serializer.WaitOne();

            PlayableData nextFile = _PlaylistGoNext();
            if (null != nextFile)
               Player.PlayFile( nextFile.filePath, nextFile.key );

            ++_changeCount;
         }
         finally
         {
            _serializer.ReleaseMutex();
         }
      }

      ///
      /// Push this file onto the queue of songs to play and start
      /// playing it
      ///
      public void GotoPrevFile()
      {
         try
         {
            _serializer.WaitOne();

            PlayableData prevFile = _PlaylistGoPrev();
            Console.WriteLine( "PREV = " + prevFile.title );
            if (null != prevFile)
               Player.PlayFile( prevFile.filePath, prevFile.key );

            ++_changeCount;
         }
         finally
         {
            _serializer.ReleaseMutex();
         }
      }


      ///
      /// Called by the mp3 reader when a track is finished. 
      ///
      /// \warning Called in the context of the reader thread!
      ///
//       void _TrackFinishedCallback(  TrackFinishedInfo info )
//       { 
//          // Previous track could be nothing?
//          if (null != info)
//          {            
//             Console.WriteLine( "Track Finished " + info.key );

//             // Because of the threading, I don't know we can guarantee
//             // this will never happen, but let's find out:
//             Debug.Assert( info.key == currentTrack.key,
//                           "finished track != playing track" );
//          }

//          // Advance the playlist
//          PlayableData nextInfo = _PlaylistGoNext();
//          if (null != nextInfo)
//          {
//             // Tell the player to play this track next
//             Player.SetNextFile( nextInfo.filePath, nextInfo.key );
//          }
//       }
   
      ///
      /// Called by the mp3 reader when a track starts playing.
      ///
      /// \warning Called in the context of the reader thread!
      ///
//       void _TrackStartingCallback( uint index, string path )
//       {
//          // This would be a good place to update our current-playing-track
//          // indicator.
//       }

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
         _serializer.WaitOne();
         try
         {
            return _database.Mp3FileEntryExists( fullPath );
         }
         finally
         {
            _serializer.ReleaseMutex();
         }
      }

      ///
      /// Adds the info for this local track-on-disk to the database
      ///
      public void Add( PlayableData newData )
      {
         _serializer.WaitOne();
         try
         {
            _database.Add( newData );
         }
         finally
         {
            _serializer.ReleaseMutex();
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
      IDbConnection  _databaseConnection;

      FileSelector   _fileSelector;

      // FileScanner    _fileScanner;

      StatusDatabase _database;

      PlaylistCriteria _criteria = new PlaylistCriteria();

   }
} // tam namespace
