/// \file
/// $Id$
///
/// Configuration interfaces
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
   using System.Data;
   using System.Diagnostics;
   using System.Threading;
   using Gtk;
   using GtkSharp;

   ///
   /// \todo Replace this with code that doesn't inherit from Gtk.Dialog,
   ///   or use glade
   ///
   public class ConfigDlg 
      : Gtk.Dialog
   {
      ///
      /// \param settings is directly modified by ConfigDlg when OK is
      ///   pressed
      ///
      public ConfigDlg( PlayerSettings settings )
      {
         _settings = settings;

         Response += new ResponseHandler( _OnResponse );

         AddButton( "Ok", (int)ResponseType.Ok );
         AddButton( "Cancel", (int)ResponseType.Cancel );

         // Now for the state editing controls:
         VBox.Add( new Label( "Suck" ) );

         _serverName = new Entry();
         _serverName.ActivatesDefault = true;
         _serverName.Text = _settings.serverName;
         VBox.Add( _serverName );

         _serverPort = new Entry();
         _serverPort.ActivatesDefault = true;
         _serverPort.Text = _settings.serverPort.ToString();
         VBox.Add( _serverPort );

         VBox.ShowAll();
      }

      void _OnResponse( object o, ResponseArgs args )
      {
         switch (args.ResponseId)
         {
         case ResponseType.Ok:         // save
            Trace.WriteLine( "ConfigDlg: Ok" );
            bool convertOk = false;
            int port = 0;
            try
            {
               port = Convert.ToInt32( _serverPort.Text );
               convertOk = true;
            }
            catch (Exception e)
            {
               // I think we should prevent all invalid entry rather
               // than catching it here after the fact.
               Trace.WriteLine( "Invalid port - " + e.ToString() );
            }
            
            if (convertOk)
            {
               _settings.serverName = _serverName.Text;
               _settings.serverPort = port;
               Hide();
            }

            break;
            
         case ResponseType.Cancel:
            // Restore previous values to controls
            _serverName.Text = _settings.serverName;
            _serverPort.Text = _settings.serverPort.ToString();
            Hide();
            break;
            
         default:
            Trace.WriteLine( "ConfigDlg: Unexpected Response" );
            break;
         }
      }

      PlayerSettings _settings;

      Entry          _serverName;
      Entry          _serverPort;
   }
}
