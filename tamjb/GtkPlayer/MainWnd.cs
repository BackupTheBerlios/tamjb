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
   using System.Runtime.Remoting;
   using System.Runtime.Remoting.Channels;
   using System.Runtime.Remoting.Channels.Http;
   using System.Runtime.Remoting.Channels.Tcp;
   using System.Threading;
   using Gtk;
   using GtkSharp;

   // Need to include things that would  normally be hidden to get
   // the data structs they use. Should these be moved into the global
   // namespace to make them available at the "tam" namespace scope?
   
   ///
   /// Gtk# frontend to TamJB -- main program
   ///
   public class EntryPoint
   {
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

            // Old:
            // MainWnd player = new MainWnd();
            // player.Show();

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
   }

}
