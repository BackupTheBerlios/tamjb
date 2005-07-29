/// \file
/// $Id$
///
/// The Main entry point and main loop of the jukebox backend.
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
//   Tom Surace <tekhedd@byteheaven.net>

namespace byteheaven.tamjb.Server
{
   using System;
   using System.Diagnostics;
   using System.IO;
   using System.Runtime.Remoting;
   using System.Runtime.Remoting.Channels;
   using System.Runtime.Remoting.Channels.Http;
   using System.Runtime.Remoting.Channels.Tcp;
   using System.Threading;
   using System.Collections;
   using System.Collections.Specialized;

   ///
   /// \todo Make SimpleMp3Player not static so it doesn't need to be
   ///   referenced here
   ///
   using byteheaven.tamjb.SimpleMp3Player;
   using byteheaven.tamjb.Engine;
   using byteheaven.tamjb.Interfaces;

   ///
   /// Server for client-server mode
   ///
   public class ServerMain
   {
      /// \todo Get initial parameters from configuration
      ///    or whatever
      static int _port = 0;
      static string _connectionString = null;
      static int QUEUE_MIN_SIZE = 6; // get ahead of ourselves.

      static int SCAN_FINISHED_SLEEP_TIME = 100; // loops

      /// 
      /// \todo The list of mp3 dirs should be changeable at runtime
      ///   via some sort of interface.
      ///
      static ArrayList _mp3RootDirs = new ArrayList();

      static void _Usage()
      {
         Console.WriteLine( "usage: " );
         Console.WriteLine( " --bufferCount" );
         Console.WriteLine( " --bufferSize" );
         Console.WriteLine( " --create (create empty database and exit)" );
         Console.WriteLine( " --dbUrl <file:/path/to.db> (or use connectionString)" );
         Console.WriteLine( " --connectionString Server=<host>;Database=<db>;User ID=<id>;Password=<pass>" );
         Console.WriteLine( " --dir <mp3_root_dir> (multiple dirs allowed)" );
         Console.WriteLine( " --lifeSpan <maxlife-in-minutes>" );
         Console.WriteLine( " --logFile <logFile>" );
         Console.WriteLine( " --port <port>" );
         Console.WriteLine( " --trace" );
      }

      ///
      /// Open logfile for writing, and return it. What could be easier? 
      ///
      /// \todo
      ///   Wouldn't it be neat to have some sort of log rotation class?
      ///
      /// \return
      ///   An object representing the log stream. And so on.
      ///
      static TextWriter _RedirectLogFile( string logFileName )
      {
         StreamWriter logWriter = 
            new StreamWriter( new FileStream( logFileName,
                                              FileMode.Create,
                                              FileAccess.Write,
                                              FileShare.Read ) );

         Console.SetOut( logWriter );
         Console.SetError( logWriter );

         return logWriter;
      }

      ///
      /// Main. Nuff said.
      ///
      /// \param args Now what, exactly, do you THINK is getting passed to
      ///   our static Main function? Hmmm?
      ///
      /// \todo As of mono 1.1.7, we could actually register as a system
      ///   service using standard api's. Should we?
      ///
      [MTAThread]
      public static int Main(string [] args) 
      {
         // The command line deals with the problem that the config
         // file can be overwritten by the GUI, but the GUI can't
         // be reached until remoting is enabled, which requires
         // the port parameter. You won't want to change that from
         // remote anyway. I hope!

         // defaults
         string logFile = "-";
         string connectionString = null;
         bool doTrace = false;
         bool createDatabase = false;
         uint bufferSize = 44100 / 4;
         uint bufferCount = 30;
         bool isImmortal = true;
         TimeSpan maxLife = new TimeSpan( 0, 0, 0 );
         _port = 0;

         for (int i = 0; i < args.Length; i++)
         {
            string arg = args [i];
				
            switch (arg)
            {
            case "--create":
               createDatabase = true;
               break;

            case "--port":
               _port = Convert.ToInt32( args[++i] );
               break;
              
            case "--logFile":
               logFile = args[++i];
               break;

            case "--dbUrl":
               connectionString = "URI=" + args[++i];
               break;

            case "--connectionString":
               connectionString = args[++i];
               break;

            case "--dir":
               _mp3RootDirs.Add( args[++i] );
               break;

            case "--trace":
               doTrace = true;
               break;
               
            case "--bufferSize":
               bufferSize = Convert.ToUInt32( args[++i] );
               break;

            case "--bufferCount":
               bufferCount = Convert.ToUInt32( args[++i] );
               break;

            case "--lifeSpan":
               int lifeSpan = Convert.ToInt32( args[++i] );
               maxLife = new TimeSpan( 0, lifeSpan, 0 );
               isImmortal = false;
               break;

            default:
               Console.WriteLine( "Unknown argument: {0}", arg );
               _Usage();
               return 2;
            }
         }

         if (null == connectionString)
         {
            Console.WriteLine( "--dbUrl or --connectionString required" );
            _Usage();
            return 2;
         }

         if (createDatabase)
         {
            _CreateDatabase( connectionString );
            return 0;
         }

         if (_port == 0)
         {
            Console.WriteLine( "--port required" );
            _Usage();
            return 2;
         }

         try
         {
            TextWriter traceWriter;
            if ("-" == logFile)
               traceWriter = Console.Out;
            else
            {
               traceWriter = 
                  TextWriter.Synchronized(_RedirectLogFile( logFile ));
            }

            if (doTrace)
            {
               // Spit all trace output to this stream
               Trace.Listeners.Add( new TextWriterTraceListener(traceWriter) );
               Trace.AutoFlush = true;
            }

            _connectionString = connectionString;

            // Configure the database engine so the global engine will
            // be constructed correctly. There's GOTTA be a better way
            // to do this.

            Backend.Init( QUEUE_MIN_SIZE, _connectionString );
            Backend.theBackend.desiredQueueSize = 20; // Get many. I mean, what if they all suck?
            Backend.theBackend.bufferSize = bufferSize;
            Backend.theBackend.bufferCount = bufferCount;
            Backend.theBackend.bufferPreload = bufferCount;

            Backend.theBackend.Poll();

            // Register as an available service for the tam Engine.
            _CreateChannel( _port );

            // The Engine object is a singlecall that references the
            // statically created Backend object: Backend.theBackend.

            RemotingConfiguration.
               RegisterWellKnownServiceType( typeof(Engine), 
                                             "Engine", 
                                             WellKnownObjectMode.SingleCall ); 

            // Force creation of the SAO by creating a local client on
            // the server that polls the server to make it enqueue files.
            // And stuff. So this thread is like a remote client of the
            // other threads in this client. Hmmm.

            // Retrieve a reference to the "remote" engine. The backend
            // can reference the actual Engine class, not just its interface.
            // string serverUrl = "http://localhost:" + _port + "/Engine";

            Trace.WriteLine( "tam.Server started on port " + _port );

            RecursiveScanner scanner = null;;
            int scannerIndex = 0;
            int scanRetryCountdown = 0;

            // Drop priority of the scanner thread. I guess.
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            DateTime startTime = DateTime.Now;
            while (true)
            {
               if ((!isImmortal) && ((DateTime.Now - startTime) > maxLife))
               {
                  Trace.WriteLine( "Reached my lifeSpan. \"Wake up, time to die.\"" );
                  Trace.WriteLine( "Exiting." );
                  Backend.theBackend.ShutDown();
                  return 0;
               }

               // Continually scan all configured dirs for new mp3's
               try
               {
                  // Don't bother if no mp3 dirs are configured
                  if (_mp3RootDirs.Count > 0)
                  {
                     if (scanRetryCountdown > 0)
                     {
                        -- scanRetryCountdown;
                     }
                     else
                     {
                        if (null == scanner)
                        {
                           string nextDir = (string)_mp3RootDirs[scannerIndex];
                           Trace.WriteLine( "Now scanning: " + nextDir );
                           
                           scanner = new RecursiveScanner( nextDir );
                        }
                        
                        Debug.Assert( scanner != null, "logic error" );
                        
                        if (scanner.DoNextFile(5, Backend.theBackend) 
                            == ScanStatus.FINISHED)
                        {
                           Trace.WriteLine( "Scan Finished" );
                           
                           ++ scannerIndex;
                           if (scannerIndex >= _mp3RootDirs.Count)
                           {
                              // Done scanning all dirs. Wait a few minutes
                              // before restarting.
                              scanRetryCountdown = SCAN_FINISHED_SLEEP_TIME;
                              scannerIndex = 0;
                           }
                           
                           scanner = null;
                        }
                     }
                  }
               }
               catch (Exception e)
               {
                  Console.WriteLine( "Error while scanning: " + e.ToString() );
               }
               
               try
               {
                  Backend.theBackend.Poll(); // keep the engine working
               }
               catch (Exception e)
               {
                  Console.WriteLine( "Poll Failed: " + e.ToString() );
               }

               Thread.Sleep( 2000 );   // wait a while

               // HACK for memory leak watching. WTF is going on?
//                GC.Collect();
//                Console.WriteLine( "Memory: {0}", GC.GetTotalMemory(true) );
            }
         }
         catch (Exception outerEx)
         {
            Console.WriteLine( "Exception in main loop" );
            Console.WriteLine( outerEx.ToString() );
         }
         finally
         {
         }

         return 3; // Should not be reached
      }

      static void _CreateDatabase( string connectionString )
      {
         StatusDatabase db = new StatusDatabase( connectionString );
         db.CreateTablesIfNecessary();
      }


      ///
      /// Set up our client-server channel
      ///
      static void _CreateChannel( int port )
      {
         ListDictionary properties = new ListDictionary();
         properties.Add( "port", port );

         // Could use Soap or Binary formatters if we wanted... 
         //  Will Binary work cross-platform? Soap is more generic but slow.
//          HttpChannel channel = 
//             new HttpChannel(properties,
//                             new BinaryClientFormatterSinkProvider(),
//                             new BinaryServerFormatterSinkProvider());

//          ChannelServices.RegisterChannel( channel );

         TcpChannel channel =
            new TcpChannel( properties,
                            new BinaryClientFormatterSinkProvider(),
                            new BinaryServerFormatterSinkProvider());
                      
         ChannelServices.RegisterChannel( channel );
      }   
   }
}
