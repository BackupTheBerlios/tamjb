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
         try
         {
            // Spit all trace output to stdout if desired
            Trace.Listeners.Add( new TextWriterTraceListener(Console.Out) );
            Trace.AutoFlush = true;

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
            
            Application.Init ();

            // New:
            GtkPlayer gtkPlayer = new GtkPlayer();

            Application.Run ();
         }
         catch ( Exception e )
         {
            // help, this is ugly! Wrap anything we expect in a more
            // useful error message wiht the exception available as detail?
            string msg = "Unexpected Exception: " + e.ToString();

            MessageDialog md = 
               new MessageDialog( null, 
                                  DialogFlags.Modal,
                                  MessageType.Error,
                                  ButtonsType.Close, 
                                  msg );
     
            int result = md.Run ();
         }
         Trace.WriteLine( "exiting" );

         // If any stray threads are around, deal with it here.
      }

      public static IEngine backend
      {
         get
         {
            if (null == _backendProxy)
            {
               if (_runLocal)
               {
                  _backendProxy = _CreateLocalEngine();
               }
               else
               {
                  // TODO: the backend could be instantiated in-process
                  //   if we don't care about running it as a daemon.
                  _backendProxy = 
                     (IEngine) Activator.GetObject( typeof(IEngine), 
                                                    _serverUrl );
               }
            }

            return _backendProxy;
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

      ///
      /// Create a backend and scanner locally instead of remotely
      ///
      /// \todo Store engine parameters in the database (and then 
      ///   perhaps move this function etc into the GUI part of the player.)
      ///
      static IEngine _CreateLocalEngine()
      {
         // Assembly id3 = Assembly.LoadWithPartialName( "byteheaven.id3" );
         Assembly engineAssembly = 
            Assembly.LoadWithPartialName( "tamjb.Engine" );
         
         Type type = Type.GetType( 
            "byteheaven.tamjb.Engine.RecursiveScanner,tamjb.Engine" );
         object [] args = new object[1];
         args[0] = "/";
         IRecursiveScanner scanner = 
            (IRecursiveScanner)Activator.CreateInstance( type, args );

         type = Type.GetType( 
            "byteheaven.tamjb.Engine.Backend,tamjb.Engine" );
         args = new object[2];
         args[0] = (int)5; //QUEUE_MIN_SIZE;
         args[1] = "foo"; // _connectionString;
         _backendProxy = (IEngine)Activator.CreateInstance( type, args );
         
         return _backendProxy;
      }


      ///
      /// Proxy object that references the server back end. If the
      /// settings change, set this to null, and the accessor will know
      /// to reconnect.
      ///
      static bool     _runLocal = false; // don't use remoting
      static IEngine  _backendProxy = null;
      static string   _serverUrl = "";



   }
}
