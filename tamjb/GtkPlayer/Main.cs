/// \file
/// $Id$
///
/// Gtk-sharp frontend to the tam jukebox.
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
//   Tom Surace <tekhedd@byteheaven.net>

namespace byteheaven.tamjb.GtkPlayer
{
   using System;
   using System.Collections;
   using System.Collections.Specialized;
   using System.Diagnostics;
   using System.Reflection;
   using System.Runtime.Remoting;
   using System.Runtime.Remoting.Channels;
   using System.Runtime.Remoting.Channels.Http;
   using System.Runtime.Remoting.Channels.Tcp;
   using System.Threading;
   using Gtk;
   using GtkSharp;

   using byteheaven.tamjb.Interfaces;

   ///
   /// Gtk# frontend to TamJB -- main program
   ///
   public class PlayerApp
   {
      [MTAThread]
      static void Main( string [] args )
      {
         _runLocal = true;

         try
         {
            BooleanSwitch traceEnableSwitch = 
               new BooleanSwitch("TraceEnable", "Enable trace messages");


            if (traceEnableSwitch.Enabled)
            {
               // Spit all trace output to stdout if desired
               Trace.Listeners.Add( new TextWriterTraceListener(Console.Out) );
            }

            Trace.AutoFlush = true;
            
            // Set up remoting services if we are using a remote server
            if (! _runLocal)
            {
               // Create a channel for communicating w/ the remote object
               // Should not have to explicitly state BinaryClient, should I?

               ListDictionary properties = new ListDictionary();
               HttpChannel channel = 
                  new HttpChannel(properties,
                                  new BinaryClientFormatterSinkProvider(),
                                  new BinaryServerFormatterSinkProvider());

               ChannelServices.RegisterChannel( channel );

               // Yeah, we support Tcp too.
               TcpChannel tcpChannel = new TcpChannel();
               ChannelServices.RegisterChannel( tcpChannel );
            }
            else                // running locally
            {
               ; // nothing to do...
            }
            
            Application.Init ();

            // New:
            GtkPlayer gtkPlayer = new GtkPlayer();

            Application.Run ();
         }
         catch ( Exception e )
         {
            // help, this is ugly! Ideas?

            Console.WriteLine( "Unexpected Exception: {0}", e.ToString() );

            // This doesn't work right. Apparently since the application
            // is not running, the OK button won't work either. :(
//             MessageDialog md = 
//                new MessageDialog( null, 
//                                   DialogFlags.Modal,
//                                   MessageType.Error,
//                                   ButtonsType.Close, 
//                                   msg );
     
//             int result = md.Run ();
         }
         Trace.WriteLine( "exiting" );

         // If any stray threads are around, deal with it here.
         if (_runLocal)
            _backendInterface.ShutDown();

         _scanner = null;
         _backendProxy = null;
         _backendInterface = null;
      }

      public static IEngine backend
      {
         get
         {
            if (null == _backendProxy)
               _Connect();

            return _backendProxy;
         }
      }

      static IBackend backendInterface
      {
         get
         {
            if (null == _backendProxy)
            {
               if (!_runLocal)
               {
                  throw new ApplicationException( 
                     "Can't create IBackend object with remote connection" );
               }
               _Connect();
            }

            return _backendInterface;
         }
      }


      public static bool isStandalone
      {
         get
         {
            return _runLocal;
         }
      }

      public static string serverUrl 
      {
         get
         {
            return _serverUrl;
         }

         set
         {
            // Set to null to force new connection on the next retrieval:
            _backendProxy = null;

            _serverUrl = value;
         }
      }


      public static string mp3RootDir
      {
         get
         {
            return _mp3RootDir;
         }
         set
         {
            _mp3RootDir = value;
            _scanner = null;    // force starting over in new dir
         }
      }

      // This probably should just be exposed as the "path"
      // URI=file://path/to/file.db
      public static string connectionString
      {
         get
         {
            return _connectionString;
         }
         set
         {
            if (null == value)
               _connectionString = "";
            else
               _connectionString = value;

          
            if (null != _backendInterface)
               _backendInterface.ShutDown();
               
            _backendProxy = null; // force reload of backend
            _backendInterface = null;
         }
      }

      ///
      /// Connect to the back end, or create local engine object if
      /// not running client-server
      ///
      static void _Connect()
      {
         if (_runLocal)
         {
            object backend = _CreateLocalEngine();
            _backendProxy = (IEngine)backend;
            _backendInterface = (IBackend)backend;
            
            // Set up the configurable parameters. :) Should not be
            // hardcoded, OK?
            _backendInterface.desiredQueueSize = 6;
            _backendInterface.bufferCount = 20;
            _backendInterface.bufferPreload = 20;
            _backendInterface.bufferSize = 8192;
         }
         else
         {
            _backendProxy = 
               (IEngine) Activator.GetObject( typeof(IEngine), 
                                              _serverUrl );
         }
      }

      ///
      /// Create a backend locally
      ///
      static object _CreateLocalEngine()
      {
         // Assembly id3 = Assembly.LoadWithPartialName( "byteheaven.id3" );
         Assembly engineAssembly = 
            Assembly.LoadWithPartialName( "tamjb.Engine" );
         
         Type type = Type.GetType( 
            "byteheaven.tamjb.Engine.Backend,tamjb.Engine" );

         if (null == type)
            throw new ApplicationException( "Cannot load Engine.Backend" );

         object [] args = new object[2];
         args[0] = (int)5; //QUEUE_MIN_SIZE;
         args[1] = _connectionString;
         return Activator.CreateInstance( type, args );
      }

      ///
      /// Create an instance of recursivescanner (using late binding)
      ///
      static IRecursiveScanner _CreateRecursiveScanner( string dir )
      {
         Assembly engineAssembly = 
            Assembly.LoadWithPartialName( "tamjb.Engine" );

         Type type = Type.GetType( 
            "byteheaven.tamjb.Engine.RecursiveScanner,tamjb.Engine" );

         if (null == type)
            throw new ApplicationException( "Cannot load RecursiveScanner" );

         object [] args = new object[1];
         args[0] = dir;

         return (IRecursiveScanner)Activator.CreateInstance( type, args );
      }
      
      
      ///
      /// If we are running locally, poll this periodically to scan
      /// for new files in the mp3 dir(s), and do other background
      /// processing in the Engine. About once per second should do it.
      ///
      public static void ScanForFiles()
      {
         // Don't bother if no mp3 dirs are configured
         if (null != _mp3RootDir)
         {
            if (null == _scanner)
            {
               string nextDir = _mp3RootDir;
               Trace.WriteLine( "Now scanning: " + nextDir );
               
               _scanner = _CreateRecursiveScanner( nextDir );
            }
                  
            Debug.Assert( _scanner != null, "logic error" );
            
            if (_scanner.DoNextFile(5, backendInterface) 
                == ScanStatus.FINISHED)
            {
               Trace.WriteLine( "Scan Finished" );
               _scanner = null;
            }
         }
      }

      public static void CreateDatabase( string connectionString )
      {
         backendInterface.CreateDatabase( connectionString );
      }

      ///
      /// Don't call this if you are running as a client to a remote
      /// engine, because it will simply throw an exception.
      ///
      public static void PollBackend()
      {
         backendInterface.Poll();
      }

      ///
      /// Proxy object that references the server back end. If the
      /// settings change, set this to null, and the accessor will know
      /// to reconnect.
      ///
      static bool      _runLocal = true; // don't use remoting?
      static IEngine   _backendProxy = null;
      static string    _serverUrl = "";

      //
      // Variables used when we are running locally
      static IBackend          _backendInterface = null;
      static IRecursiveScanner _scanner = null;
      static int               _scanRetryCountdown = 0;

      // Things that are configurable when running locally
      static string            _mp3RootDir = null;
      static string            _connectionString = "(unknown)";
   }
}
