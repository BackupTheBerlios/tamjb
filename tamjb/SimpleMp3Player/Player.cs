/// \file
/// $Id$
///
/// A class that streams an audio file to "the speakers". Not well
/// thought out.
///
/// The main reason for this wrapper is that the streaming
/// interfaces available to c# are very platform-specific. I expect
/// this to get better.
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
// Feel free to track down and contact ALL project contributors to
// negotiate other terms. Bring a checkbook.
//
//   Tom Surace <tekhedd@byteheaven.net>


namespace tam.SimpleMp3Player
{
   using System;
   using System.IO;
   using System.Diagnostics;
   using System.Collections;
   using System.Threading;
   using EsdSharp;                 // Esd kinda sucks, but it works!
   using Mp3Sharp;                 // Inefficient. But it work!

   ///
   /// Event handler called when track is done playing. This is only
   /// called if the track ends because of the engine (end of file,
   /// error, and so on). If a track is terminated by calling
   /// PlayFile or GotoNextFile or whatever, this is not called.
   ///
   /// \param status Indicates how/why the track finished (whether
   ///   we just finished playing, or it failed for some other reason).
   ///
   public delegate void TrackFinishedHandler( TrackFinishedInfo status );

   public delegate void TrackStartingHandler( uint nextTrackId,
                                              string path );
   
   ///
   /// A struct to hold info about enqueued tracks
   ///
   class TrackInfo
   {
      public string path;
      public uint   index;
      
      public TrackInfo( string p, uint i )
      {
         path = p;
         index = i;
      }
   }

   ///
   /// A class that plays and controls the playback of streams
   /// from disk. The existance of files and their status are stored
   /// in the LocalFileDatabase.
   ///
   /// \todo Can somebody please tell me why I made everything static?
   ///
   public class Player
   {
      readonly static int AUDIO_TIMEOUT = 10000; // milliseconds?
      static Player()
      {
         // This is only necessary because calling the event handler with
         // no listeners connected causes a NullReferenceException. Easy fix:
         // install a local listener
         OnTrackFinished += new TrackFinishedHandler( _MyOnTrackFinished );
      }
      static void _MyOnTrackFinished( TrackFinishedInfo status )
      {
         // Really...do nothing. No, really.
      }

      /// 
      /// Buffer size must be set to a nice round number. Well, not really.
      /// in fact it's irrelevant. This could change in the future though. :P
      ///
      /// This size is in samples for all channels (whether stereo or whatever).
      ///
      public static uint bufferSize 
      {
         get 
         {
            return _bufferSize;
         }

         set 
         {
            _bufferSize = value;
         }
      }

      public static bool isPlaying
      {
         get
         {
            return State.STOP != _state;
         }
      }

      public static string currentTrack
      {
         get
         {
            _configMutex.WaitOne();
            string val = _playingTrack.path;
            _configMutex.ReleaseMutex();

            return val;
         }
      }

      public static int playQueueSize
      {
         get
         {
            Queue synced = Queue.Synchronized( _playFilesQueue );
            return synced.Count;
         }
      }

      public static uint currentTrackIndex
      {
         get
         {
            _configMutex.WaitOne();
            uint val = _playingTrack.index;
            _configMutex.ReleaseMutex();

            return val;
         }
      }

      // Lots of buffers are good for my crappy laptop. This is a jukebox 
      // not a soft synth, right?
      static uint _buffersInQueue = 20;
      public static uint buffersInQueue
      {
         set
         {
            _buffersInQueue = value;
            if (_buffersInQueue < 2)
               _buffersInQueue = 2;

            if (_buffersToPreload > _buffersInQueue)
               _buffersToPreload = _buffersInQueue;
         }
         get
         {
            return _buffersInQueue;
         }
      }

      /// If this is greater than buffersInQueue, audio will not happen
      static uint _buffersToPreload = 5;
      public static uint buffersToPreload
      {
         set
         {
            if (value > _buffersInQueue) // should this throw?
               _buffersToPreload = _buffersInQueue;
            else
               _buffersToPreload = value;
         }

         get
         {
            return _buffersToPreload;
         }
      }

      /// 
      /// Shuts down the playing engine and kills the reader and
      /// audio streamer threads. Call before program exit if you
      /// want the program to exit. :)
      ///
      public static void ShutDown()
      {
         _KillReaderThread();
         _esd = null;
      }

      ///
      /// Starts playback of the indicated file. This clears out any
      /// existing queue of files to play and immediately starts
      /// playing this file instead.
      ///
      /// \todo Implement crossfades and other goodies
      ///
      public static void PlayFile( string path, uint index )
      {
         Debug.Assert( null != path ); // just plain bad parameter
         Trace.WriteLine( "PlayFile", "Player" );

         _StartReaderThread();

         Queue synced = Queue.Synchronized( _playFilesQueue );
         synced.Clear();
         synced.Enqueue( new TrackInfo( path, index ) );

         ///
         /// Wait for the buffer thread to finish its current operation:
         ///
         _configMutex.WaitOne();
         try
         {
            _state = State.PLAY_FILE_REQUEST;
            _fileToPlayEvent.Set();
         }
         catch (Exception e)
         {
            _state = State.STOP;
            throw new ApplicationException( "Could not start playback", e );
         }
         finally
         {
            _configMutex.ReleaseMutex();
         }
      }

      ///
      /// Stops playing the current track, and starts the next one
      /// in the play queue. If no files are in the queue, playback
      /// should stop. (But you never know!)
      ///
      static public void GotoNextFile()
      {
         Trace.WriteLine( "GotoNextFile", "Player" );

         _StartReaderThread();

         _configMutex.WaitOne();
         try
         {
            switch (_state)
            {
            case State.PLAYING:
               _state = State.PLAY_FILE_REQUEST;
               _fileToPlayEvent.Set();
               break;
               
            case State.PLAY_FILE_REQUEST: // do nothing
               break;

            default:
               /// \todo Handle advancing in the playlist when stopped
               ///
               break; // Ignore for now
            }
         }
         finally
         {
            _configMutex.ReleaseMutex();
         }
      }

      ///
      /// Set the next file to be played after the current one
      /// finishes. Clears out anything else in the queue but
      /// does not end the current playing track.
      ///
      public static void SetNextFile( string path, uint index )
      {
         Debug.Assert( null != path, "just plain bad parameter" );
         Trace.WriteLine( "SetNextFile", "Player" );

         _StartReaderThread();

         Queue synced = Queue.Synchronized( _playFilesQueue );
         synced.Clear();
         synced.Enqueue( new TrackInfo( path, index ) );
         synced = null;

         _StartReaderThread();  // just in case it's stopped

         _configMutex.WaitOne();
         try
         {
            if (_state == State.STOP)
            {
               _state = State.PLAY_FILE_REQUEST;
               _fileToPlayEvent.Set();
            }
         }
         catch (Exception e)
         {
            _state = State.STOP;
            throw new ApplicationException( "Could not start playback", e );
         }
         finally
         {
            _configMutex.ReleaseMutex();
         }

      }

      ///
      /// Add a file to the queue of files to be played.
      ///
      public static void EnqueueFile( string path, uint index )
      {
         Debug.Assert( null != path ); // just plain bad parameter
         Trace.WriteLine( "EnqueueFile", "Player" );

         _StartReaderThread();  // just in case it's stopped

         Queue synced = Queue.Synchronized( _playFilesQueue );
         synced.Enqueue( new TrackInfo( path, index ) );

         _configMutex.WaitOne();
         try
         {
            if (_state == State.STOP)
            {
               _state = State.PLAY_FILE_REQUEST;
               _fileToPlayEvent.Set();
            }
         }
         catch (Exception e)
         {
            _state = State.STOP;
            throw new ApplicationException( "Could not start playback", e );
         }
         finally
         {
            _configMutex.ReleaseMutex();
         }
      }

      public static event TrackFinishedHandler OnTrackFinished;
      public static event TrackStartingHandler OnTrackPlayed;

      ///
      /// Launch the mp3 reader thread if it is not running
      ///
      static void _StartReaderThread( )
      {
         if (null == _esd)
            _esd = new Esd();

         _configMutex.WaitOne();
         try
         {
            if (null == _mp3ReaderThread 
                || !_mp3ReaderThread.IsAlive)
            {
               _mp3ReaderThread = 
                  new Thread( new ThreadStart( _Mp3ReaderThread ) );

               // _mp3ReaderThread.Priority = ThreadPriority.Highest;
               _mp3ReaderThread.Start();
            }
         }
         finally
         {
            // Mustn't forget!
            _configMutex.ReleaseMutex();
         }
      }

      ///
      /// Stop the reader thread if it is running. Etc.
      ///
      static void _KillReaderThread()
      {
         if (null != _mp3ReaderThread && _mp3ReaderThread.IsAlive)
         {
            _configMutex.WaitOne();
            try
            {
               _state = State.SHUTDOWN_REQUEST;
               
               // Release the mp3 thread if it's "done"
               _fileToPlayEvent.Set();
            }
            finally
            {
               _configMutex.ReleaseMutex();
            }

            // Wait for the mp3 thread to exit. For a while, depending
            // on the current buffer size.

            if (!_mp3ReaderThread.Join( (int)_bufferSize / 2 ))
            {
               // Thread didn't exit; terminate it with prejudice.
               _mp3ReaderThread.Abort();
            }
            _mp3ReaderThread = null;
         }
      }

      ///
      /// This is the entry point for the buffer processing thread.
      ///
      /// \todo This needs exception handling.
      ///
      static void _Mp3ReaderThread()
      {
         Trace.WriteLine( "Hello", "MP3" );

         Thread audioThread = null;
         try
         {
            ///
            /// \todo Should get the sample rate from the mp3 file's header 
            ///    info instead of fixing it at 44100.
            ///
            
            Trace.WriteLine( "Starting esd", "MP3" );
            ///
            /// \todo Exceptions won't propagate back to the parent 
            ///   thread. How do we indicate esd errors?
            ///
            _estream = _esd.PlayStream( "Generic", 
                                        EsdChannels.Stereo,
                                        44100,
                                        EsdBits.Sixteen );

            _estream.AllocWriteBuffer( (int)_bufferSize );
            
            audioThread = new Thread( new ThreadStart( _AudioThread ) );
            // audioThread.Priority = ThreadPriority.BelowNormal;
            audioThread.Start();

            Trace.WriteLine( "Entering main loop", "MP3" );
            _configMutex.WaitOne();
            while (true)
            {
               // Execution will stop here if the parent thread tries to 
               // change the configuration in some way.
               _configMutex.ReleaseMutex();
               _configMutex.WaitOne();
               
               switch (_state)
               {
               case State.STOP:
                  Trace.WriteLine( "STOP", "MP3" );
                  
                  // Wait until the parent wakes us up.
                  _configMutex.ReleaseMutex();
                  _fileToPlayEvent.WaitOne();
                  Trace.WriteLine( "STOP -> " + _state.ToString(), "MP3" );
                  _configMutex.WaitOne();
                  
                  // We've got the  _configMutex, so no race condition exists.
                  // I think. Maybe.
                  _fileToPlayEvent.Reset();
                  break;
                  
               case State.PLAY_FILE_REQUEST:
                  Trace.WriteLine( "PLAY_FILE_REQUEST", "MP3" );
                  
                  if (_bufferSizeChanged)
                  {
                     try
                     {
                        // Stop the audio writer stream to handle buffer resize. 
                        if (false == _audioThreadMutex.WaitOne( AUDIO_TIMEOUT, 
                                                                false ))
                        {
                           // If this happened, we probably want to kill the
                           // audio thread and restart esd. :(
                           throw new ApplicationException( "Timed out waiting for the audio mutex" );
                        }

                        ///
                        /// \todo Set sample rate here as well
                        ///
                        Trace.WriteLine( "creating buffers!", "MP3" );

                        // esd#'s buffer must be as large or larger than 
                        // our buffer
                        _estream.FreeWriteBuffer();
                        _estream.AllocWriteBuffer( (int)_bufferSize );

                        _CreateBuffers();
                        _underflowEvent.Set();
                        _bufferSizeChanged = false; // no longer
                     }
                     finally
                     {
                        _audioThreadMutex.ReleaseMutex();
                     }
                  }

                  // Start playing if we can.
                  if (_InternalStartNextFile() == false)
                     _state = State.STOP; // Nothing in the queue
                  else
                     _state = State.PLAYING;

                  break;
                  
               case State.PLAYING:
                  // Trace.WriteLine( "MP3> PLAYING", "MP3" );
                  
                  // Wait for a free audio buffer
                  Buffer buffer = _WaitForAndPopFreeBuffer();

                  // The Mp3Stream wrapper is still a bit flaky. Especially
                  // it seems to throw exceptions at end-of-file sometimes,
                  // probably trying to read garbage. Encase it in a
                  // try/catch block:
                  Exception playbackException = null;
                  try
                  {
                     // Fill the buffer with goodness.
                     buffer.validBytes = 
                        _mp3Stream.Read( buffer.mp3Buffer,
                                         0, 
                                         buffer.mp3Buffer.Length );
                  }
                  catch (Exception e)
                  {
                     // I'm not sure, but I think we may have to destroy
                     // and recreate the Mp3Stream to get it working again
                     // here.
                     Trace.WriteLine( "Problem Reading from MP3:" 
                                        + e.ToString(), 
                                      "MP3" );

                     // Flag the stream as finished. Heh!
                     buffer.validBytes = 0;

                     playbackException = e; // save for reporting
                  }
                  
                  if (buffer.validBytes <= 0)
                  {
                     _PushFreeBuffer( buffer );

                     // Done with prev file: notify any listeners. Send them
                     // the exception if something went wrong

                     TrackFinishedInfo info;
                     if (null == playbackException)
                     {
                        info = new TrackFinishedInfo( _playingTrack.index );
                     }
                     else
                     {
                        info = new TrackFinishedInfo( _playingTrack.index, 
                                                      playbackException );
                     }
                     OnTrackFinished( info );

                     if (_InternalStartNextFile() == false)
                        _state = State.STOP; // end of file
                  }
                  else
                  {
                     _PushMp3Buffer( buffer ); // Loaded with sample goodness
                  }
                  break;
                  
               case State.SHUTDOWN_REQUEST:
                  Trace.WriteLine( "SHUTDOWN_REQUEST", "MP3" );
                  _configMutex.ReleaseMutex();
                  // Handle cleanup in the "finally" block
                  return;
                     
               default:
                  break;
               }
            }
         }
         finally
         {
            if (null != audioThread)
            {
               // Try to get the audio thread mutex, but eventually give up
               // so we can clean up regardless.
               _audioThreadMutex.WaitOne( (int)_bufferSize / 10, false );

               ///
               /// \todo Shut down audioThread properly (Join it) instead 
               ///    of calling Abort().
               ///
               audioThread.Abort(); 
            }

            if (null != _estream)
            {
               _estream.FreeWriteBuffer();
               _estream.Close();
            }

            Trace.WriteLine( "bye", "MP3" );
         }
      }

      static void _CreateBuffers()
      {
         _mp3QueueMutex.WaitOne();
         _mp3BufferQueue.Clear(); // empty!
         _mp3BuffersEvent.Reset(); // no buffers queued
         _mp3QueueMutex.ReleaseMutex();


         _freeQueueMutex.WaitOne();

         // Create the buffers
         /// \todo should the number of buffers not be hardcoded here?
         ///

         _freeBufferQueue.Clear();
         for (int i = 0; i < _buffersInQueue; i++)
         {
            uint sizeInSamples = _bufferSize / 4;
            _freeBufferQueue.Enqueue( new Buffer( sizeInSamples ) );
         }

         _freeBuffersEvent.Set(); // There are now buffers available!
         _freeQueueMutex.ReleaseMutex();
      }

      ///
      /// Starts playing the next file, or returns false if the queue
      /// is empty (or other errors occur)
      ///
      static bool _InternalStartNextFile()
      {
         while (true)
         {
            // Queue empty?
            Queue synced = Queue.Synchronized( _playFilesQueue );
            if (synced.Count == 0)
            {
               Trace.WriteLine( "_InternalStartNextFile: queue empty" );
               if (_mp3Stream != null)
               {
                  _mp3Stream.Close();
                  _mp3Stream = null;
               }
               return false;
            }
               
            // It is assumed we own the config mutex here.
            TrackInfo info = null;
            try
            {
               Trace.WriteLine( "_InternalStartNextFile" );

               info = (TrackInfo)synced.Dequeue(); 
               _playingTrack.path = info.path;
               _playingTrack.index = info.index;
            
               FileStream stream = new FileStream( _playingTrack.path, 
                                                   FileMode.Open );

               // Note: there si a handy Mp3Stream(FileName) constructor,
               //   but if it throws an exception (say, file not found),
               //   the _mp3Stream class leaves around stray threads and
               //   the program won't exit! 

               // If the old _mp3Stream was around, it's going away now!
               if (_mp3Stream != null)
                  _mp3Stream.Close();

               _mp3Stream = new Mp3Stream( stream, _mp3ChunkSize );

               return true;
            }
            catch (Exception e)
            {
               // An exception could simply mean that the _playFilesQueue is 
               // empty, in this case trackInfo should still be null.

               if (null == info)
               {
                  Trace.WriteLine( "Queue is empty: " + e.ToString() );
               }
               else
               {
                  Trace.WriteLine( "Error opening file: " + e.ToString() );

                  // We dequeued a track but couldn't play it. Notify
                  // the player of the track's "finished with error" status.
                  ///
                  /// \todo differentiate between file-not-found errors
                  ///   and problems with the mp3 playback engine
                  ///
                  OnTrackFinished( new TrackFinishedInfo( info.index, e ) );
               }

               // If anything went wrong, park the playback engine and 
               // pass the buck.
               if (null != _mp3Stream)
               {
                  _mp3Stream.Close(); // be sure to free resources
                  _mp3Stream = null;
               }
            }
         }
      }

      ///
      /// _Mp3ReaderThread helper function
      ///
      static Buffer _WaitForAndPopFreeBuffer()
      {
         if (! _freeBuffersEvent.WaitOne(0, false)) // Out of buffers to play?
         {
            // Trace.WriteLine( "+FREE Underflow" ); // this is usually ok
         }

         // Wait (using this fine object) until there are free buffers
         _freeBuffersEvent.WaitOne();

         // There's a race here--but there should only be one process
         // waiting for free buffers!
         _freeQueueMutex.WaitOne();

         // Pop and return a free buffer
         Buffer nextBuffer = (Buffer)_freeBufferQueue.Dequeue();
         if (_freeBufferQueue.Count <= 0)
            _freeBuffersEvent.Reset(); // no free buffers left Others must wait

         _freeQueueMutex.ReleaseMutex();
         
         return nextBuffer;
      }

      ///
      /// _Mp3ReaderThread helper function
      ///
      static Buffer _WaitForAndPopMp3Buffer()
      {
         // Trace.WriteLine( "_WaitForMp3" );

         // Wait (using this fine object) until there are free buffers
         if (! _mp3BuffersEvent.WaitOne(0, false)) // Out of buffers to play?
         {
            // We have to deal with underflow here. Ick.
            _underflowEvent.Set();
            // Trace.WriteLine( "+MP3 Underflow" ); // This is usually bad
         }

         while (true)
         {
            _mp3BuffersEvent.WaitOne();

            // full buffers. I hope.
            _mp3QueueMutex.WaitOne();

            // There's a race here. If the main thread emptied the queue
            // during this tiny block, catch the error and retry.
            try 
            {
               // Pop and return a free buffer
               Buffer nextBuffer = (Buffer)_mp3BufferQueue.Dequeue();

               // If no free buffers left, block on the next call.
               if (_mp3BufferQueue.Count <= 0)
                  _mp3BuffersEvent.Reset(); 

               return nextBuffer;
            }
            catch (System.InvalidOperationException ioe)
            {
               // Queue is possibly empty. Keep trying.
            }
            finally
            {
               _mp3QueueMutex.ReleaseMutex();
            }
         }

         // not reached
      }

      static void _PushFreeBuffer( Buffer buffer )
      {
         _freeQueueMutex.WaitOne();

         _freeBufferQueue.Enqueue( buffer );
         _freeBuffersEvent.Set(); // Buffers 4 all!

         _freeQueueMutex.ReleaseMutex();
      }

      static void _PushMp3Buffer( Buffer buffer )
      {
         _mp3QueueMutex.WaitOne();

         _mp3BufferQueue.Enqueue( buffer );

         // If we had an underflow, or are just starting, don't signal
         // the buffers-available mutex until we've preloaded some buffers.
         if (_underflowEvent.WaitOne( 0, false ))
         {
            if (_mp3BufferQueue.Count >= _buffersToPreload)
               _mp3BuffersEvent.Set();
         }
         else
         {
            _mp3BuffersEvent.Set(); // Buffers 4 all!
         }

         _mp3QueueMutex.ReleaseMutex();
      }

      ///
      /// This thread tirelessly pushes audio data to the audio reader.
      ///
      static void _AudioThread()
      {
         try
         {
            Trace.WriteLine( "hello", "AUD" );
         
            while (true)
            {
               // Wait for a buffer to be ready to play
            
               Buffer buffer = _WaitForAndPopMp3Buffer();
            
               if (false == _audioThreadMutex.WaitOne( AUDIO_TIMEOUT, false ))
               {
                  Trace.WriteLine( "Timed out waiting for the audio mutex",
                                   "AUD" );
                  continue;  // keep trying
               }

               try
               {
                  int written = 0;
                  while (written < buffer.validBytes)
                  {
                     int actual = _estream.Write( buffer.mp3Buffer,
                                                  written, 
                                                  buffer.validBytes - written );

                     if (actual <= 0)
                     {
                        Trace.WriteLine( "Warning: EsdSharp.Write returned <= 0, ("
                                         + actual + ")", 
                                         "AUD" );
                        break;
                     }

                     written += actual;
                  }
               }
               finally
               {
                  _audioThreadMutex.ReleaseMutex();
               }

               // done! Well, probably. esd is strange.
               _PushFreeBuffer( buffer ); 
            }
         }
         catch (Exception ex)
         {
            // Not reached?
            Trace.WriteLine( "Warning: exiting", "AUD" );
            Trace.WriteLine( ex.ToString(), "AUD" );
         }
      }

      ///
      /// Our connection to the esd
      ///
      static Esd            _esd = null;

      ///
      /// The audio device that is currently receiving output
      ///
      static EsdStream      _estream;

      static uint           _bufferSize = 44100; // bytes, not samples.

      ///
      /// Size of read chunk (to allow for backing up, etc)
      /// The default size of 4096 causes serious system overload
      /// on my little celeron laptop.
      ///
      /// This is for the Mp3Stream class, obviously.
      ///
      static int            _mp3ChunkSize = 4096;

      ///
      /// Queue of file names to be played (strings)
      ///
      static Queue          _playFilesQueue = new Queue();

      // Name of currently playing track
      static TrackInfo      _playingTrack = new TrackInfo( "", 0 );

      ///
      /// Mp3 stream reader object
      ///
      static Mp3Stream      _mp3Stream;

      ///
      /// Buffer for reading from the mp3 file. 16-bit audio.
      //
      struct Buffer
      {
         public byte [] mp3Buffer;
         public int     validBytes;   // number of valid bytes in mp3Buffer

         public Buffer( uint size )     // size in samples
         {
            /// \todo buffer should not be hardcoded 16-bit stereo
            ///

            mp3Buffer = new byte[size * 2 * 2];
            validBytes = 0;
         }
      }

      // If this is true, buffers will be destroyed and recreated at the
      // next PLAY_FILE_REQUEST
      static bool _bufferSizeChanged = true;

      //
      // Queues and all the things needed to make 'em thread safe. Er.
      // 
      static Queue            _mp3BufferQueue = new Queue();
      static Queue            _freeBufferQueue = new Queue();

      static ManualResetEvent _mp3BuffersEvent = new ManualResetEvent( false );
      static ManualResetEvent _freeBuffersEvent = new ManualResetEvent( false );

      static Mutex            _mp3QueueMutex = new Mutex();
      static Mutex            _freeQueueMutex = new Mutex();

      // And my favorite. Ugh!
      static ManualResetEvent _underflowEvent = new ManualResetEvent( true );


      static Thread         _mp3ReaderThread;

      enum State : int
      {
         STOP,                  // nothing is happening
         PLAY_FILE_REQUEST,     // playback of new file requested
         PLAYING,               // system is playing
         SHUTDOWN_REQUEST,      // Thread is requested to exit
         ERROR                  // You are unhappy
      };

      ///
      /// Playback engine state. May be changed by whoever owns the _configMutex
      ///
      static State _state = State.STOP;

      ///
      /// State and so on may change while the parent process holds the _configMutex
      ///
      static Mutex            _configMutex = new Mutex();

      static Mutex            _audioThreadMutex = new Mutex();

      ///
      /// The MP3Reader thread will wait on this when there is nothing to
      /// play. (state = STOP)
      ///
      static ManualResetEvent _fileToPlayEvent = new ManualResetEvent( false );

   }
}
