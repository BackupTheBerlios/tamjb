/// \file
/// $Id$
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
   using Glade;

   using byteheaven.tamjb.Interfaces;

   ///
   /// A class to be connected to the Glade main window
   ///
   public class ConfigDialog
   {
      public bool isOk
      {
         get
         {
            return _isOk;
         }
      }

      public string serverName
      {
         get
         {
            return _serverName;
         }
      }

      public int serverPort
      {
         get
         {
            return _serverPort;
         }
      }

      ///
      /// Creates the mood window dialog but does not display it. 
      /// (use Run()).
      ///
      /// cred and mood are directly modified on successful validation.
      ///
      public ConfigDialog( Gtk.Window        parent,
                           string            hostName,
                           string            port )
      {
         Glade.XML glade = new Glade.XML( null,
                                          "tam.GtkPlayer.exe.glade",
                                          "_configDialog",
                                          null );

         glade.Autoconnect( this );
         
         Debug.Assert( null != _configDialog );
         Debug.Assert( null != _hostnameEntry );
         Debug.Assert( null != _portEntry );

         _configDialog.TransientFor = parent;

         _hostnameEntry.Text = hostName;
         _portEntry.Text = port;
      }

      public void Run()
      {
         _configDialog.Run();
      }

      void _OnUserResponse( object sender, ResponseArgs args )
      {
         _Trace( "[_OnUserResponse]" );

         switch (args.ResponseId)
         {
         case ResponseType.Ok:
            try
            {
               try
               {
                  _serverPort = Convert.ToInt32( _portEntry.Text );
               }
               catch (Exception e)
               {
                  _portEntry.GrabFocus();
                  return;
               }

               _serverName = _hostnameEntry.Text;

               // If we got here, the controls validated
               _configDialog.Destroy();
               _isOk = true;
            }
            catch (Exception exception)
            {
               _Trace( exception.ToString() );
            }
            break;
            
         case ResponseType.Cancel:
         default:
            _configDialog.Destroy();
            break;              // er
         }
      }

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "ConfigWindow" );
      }

      bool _isOk = false;
      string _serverName;
      int    _serverPort;

      [Glade.Widget]
      Dialog _configDialog;

      [Glade.Widget]
      Entry  _hostnameEntry;

      [Glade.Widget]
      Entry  _portEntry;

   }
}
