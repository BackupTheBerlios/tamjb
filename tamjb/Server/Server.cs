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
// Feel free to track down and contact ALL project contributors to
// negotiate other terms. Bring a checkbook.
//
//   Tom Surace <tekhedd@byteheaven.net>

namespace tam.Server
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
   using tam.SimpleMp3Player;

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

      /// 
      /// \todo The list of mp3 dirs should be changeable at runtime
      ///   via some sort of interface.
      ///
      static ArrayList _mp3RootDirs = new ArrayList();

      static void _Usage()
      {
         Console.WriteLine( "usage: " );
         Console.WriteLine( " --dbUrl <file:/path/to.db>" );
         Console.WriteLine( " --port <port>" );
         Console.WriteLine( " --logFile <logFile>" );
         Console.WriteLine( " --dir <mp3_root_dir> (multiple dirs allowed)" );
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
      public static int Main(string [] args) 
      {
         // The command line deals with the problem that the config
         // file can be overwritten by the GUI, but the GUI can't
         // be reached until remoting is enabled, which requires
         // the port parameter. You won't want to change that from
         // remote anyway. I hope!

         // defaults
         string logFile = "-";
         string dbUrl = null;
         _port = 0;

         for (int i = 0; i < args.Length; i++)
         {
            string arg = args [i];
				
            switch (arg)
            {
            case "--port":
               _port = Convert.ToInt32( args[++i] );
               break;
              
            case "--logFile":
               logFile = args[++i];
               break;

            case "--dbUrl":
               dbUrl = args[++i];
               break;

            case "--dir":
               _mp3RootDirs.Add( args[++i] );
               break;

            default:
               Console.WriteLine( "Unknown argument: {0}", arg );
               _Usage();
               return 2;
            }
         }

         if (_port == 0)
         {
            Console.WriteLine( "--port required" );
            _Usage();
            return 2;
         }

         if (null == dbUrl)
         {
            Console.WriteLine( "--dbUrl required" );
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
            
            // Spit all trace output to logfile, unless 
            Trace.Listeners.Add( new TextWriterTraceListener(traceWriter) );
            Trace.AutoFlush = true;

            _connectionString = "URI=" + dbUrl;

            // Configure the database engine so the global engine will
            // be constructed correctly. There's GOTTA be a better way
            // to do this.

            Engine.connectionString = _connectionString;
            Engine.desiredQueueSize = QUEUE_MIN_SIZE;

            // Register as an available service for the tam Engine.
            _CreateChannel( _port );

            // Every incoming message is serviced by the same object, which
            // should be in memory always. I hope.

            RemotingConfiguration.
               RegisterWellKnownServiceType( typeof(Engine), 
                                             "Engine", 
                                             WellKnownObjectMode.Singleton ); 

            // Force creation of the SAO by creating a local client on
            // the server that polls the server to make it enqueue files.
            // And stuff. So this thread is like a remote client of the
            // other threads in this client. Hmmm.

            // Retrieve a reference to the "remote" engine. The backend
            // can reference the actual Engine class, not just its interface.
            string serverUrl = "http://localhost:" + _port + "/Engine";
            Engine engine = (Engine) Activator.GetObject( typeof(Engine), 
                                                          serverUrl );

            Trace.WriteLine( "tam.Server started on port " + _port );

            RecursiveScanner scanner = null;;
            int scannerIndex = 0;
            if (_mp3RootDirs.Count > 0)
            {
               scanner = new RecursiveScanner( (string)_mp3RootDirs[0], 
                                               engine );
            }

            // Drop this thread's priority--this is not very important!
            System.Threading.Thread.CurrentThread.Priority = 
               ThreadPriority.BelowNormal;
            while (true)
            {
               // Continually scan all configured dirs for new mp3's
               try
               {
                  // Don't bother if no mp3 dirs are configured
                  if (_mp3RootDirs.Count > 0)
                  {
                     Debug.Assert( scanner != null, "logic error" );

                     if (scanner.DoNextFile(5) == ScanStatus.FINISHED)
                     {
                        ++ scannerIndex;
                        if (scannerIndex >= _mp3RootDirs.Count)
                           scannerIndex = 0;

                        string nextDir = (string)_mp3RootDirs[scannerIndex];
                        scanner = new RecursiveScanner( nextDir, engine );
                     }
                  }
               }
               catch (Exception e)
               {
                  Console.WriteLine( "Drat! " + e.ToString() );
               }
               
               try
               {
                  // Enqueue some songs cause I can
                  engine.Poll();
               }
               catch (Exception e)
               {
                  Console.WriteLine( "Drat! " + e.ToString() );
               }

               Thread.Sleep( 500 );   // wait a while
            }
         }
         finally
         {
            // Ensure this is stopped, regardless, or the app may not
            // really exit (does this need to be static?)
            SimpleMp3Player.Player.ShutDown();
         }

         // Unreachable?
         // return 3;
      }

      ///
      /// Set up our client-server channel
      ///
      static void _CreateChannel( int port )
      {
         ListDictionary properties = new ListDictionary();
         properties.Add( "port", port );

         // Could use Soap or Binary formatters if we wanted... 
         //  Will Binary work cross-platform? Soap is more reliable.
         HttpChannel channel = 
            new HttpChannel(properties,
                            new BinaryClientFormatterSinkProvider(),
                            new BinaryServerFormatterSinkProvider());

         ChannelServices.RegisterChannel( channel );
      }   
   }
}
