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
   using System.IO;
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
   /// A dialog to configure the local database connection. This is not
   /// used when running client-server. Although it could be if the client
   /// was allowed to change the server settings. Which might be dangerous.
   ///
   public class DatabaseConfigDialog
   {
      public bool isOk
      {
         get
         {
            return _isOk;
         }
      }

      public string connectString
      {
         get
         {
            return _connectString;
         }
      }

      public string mp3RootDir
      {
         get
         {
            return _mp3RootDir;
         }
      }

      ///
      /// Creates the mood window dialog but does not display it. 
      /// (use Run()).
      ///
      /// cred and mood are directly modified on successful validation.
      ///
      public DatabaseConfigDialog( Gtk.Window        parent,
                                   string            connectString,
                                   string            rootDir )
      {
         Glade.XML glade = new Glade.XML( null,
                                          "tam.GtkPlayer.exe.glade",
                                          "_databaseConfigDialog",
                                          null );

         glade.Autoconnect( this );
         
         Debug.Assert( null != _databaseConfigDialog );

         _databaseConfigDialog.TransientFor = parent;

         _connectStringEntry.Text = connectString;
         _mp3RootDirEntry.Text = rootDir;
      }

      public void Run()
      {
         _databaseConfigDialog.Run();
      }

      void _OnUserResponse( object sender, ResponseArgs args )
      {
         _Trace( "[_OnUserResponse]" );

         switch (args.ResponseId)
         {
         case ResponseType.Ok:
            try
            {
               _connectString = _connectStringEntry.Text;
               _mp3RootDir = _mp3RootDirEntry.Text;

               // Complain if the mp3 root dir doesn't exist.
               DirectoryInfo mp3DirInfo = new DirectoryInfo( _mp3RootDir );
               if (!mp3DirInfo.Exists)
               {
                  MessageDialog md = 
                     new MessageDialog( _databaseConfigDialog, 
                                        DialogFlags.Modal,
                                        MessageType.Error,
                                        ButtonsType.Ok, 
                                        ("Directory '" + _mp3RootDir 
                                         + "' does not exist") );
                  md.Run();
                  md.Destroy();
                  return;
               }

               // If we got here, the controls validated
               _databaseConfigDialog.Destroy();
               _isOk = true;
            }
            catch (Exception exception)
            {
               _Trace( exception.ToString() );
            }
            break;
            
         case ResponseType.Cancel:
         default:
            _databaseConfigDialog.Destroy();
            break;              // er
         }
      }

      void _OnMp3BtnClick( object sender, EventArgs args )
      {
         // The GTK widget is not well suited to selecting directories
         // right now. I'll do the best I can for the moment:
         FileSelection dlg = new FileSelection( "Enter MP3 Files Root Dir" );
         dlg.Filename = _mp3RootDirEntry.Text;
         dlg.SelectionEntry.Text = ".";

         // Try to make this into a directory selection dialog. :/
         dlg.HideFileopButtons();
         dlg.FileList.Parent.Visible = false;
         dlg.SelectionEntry.Visible = false;

         if ((ResponseType)dlg.Run() == ResponseType.Ok)
            _mp3RootDirEntry.Text = dlg.Filename;

         dlg.Destroy();
      }


      ///
      /// Attempt to create the database tables, etc
      ///
      void _OnCreateDatabaseBtnClick( object sender, EventArgs args )
      {
         _Trace( "[_OnCreateDatabaseBtnClick]" );
         try
         {
            string confirmPrompt = 
               "Warning: this will delete your current database!\n"
               + "Confirm: Create database tables?";

            MessageDialog confirmDialog = 
               new MessageDialog( _databaseConfigDialog,
                                  DialogFlags.Modal,
                                  MessageType.Question,
                                  ButtonsType.YesNo, 
                                  confirmPrompt );
            ResponseType response = (ResponseType)confirmDialog.Run();
            confirmDialog.Destroy();
            confirmDialog = null;

            if (ResponseType.No == response)
               return;          // << quick exit <<

            // Actually do the creation here. 
            PlayerApp.CreateDatabase( _connectStringEntry.Text );

            MessageDialog md = 
               new MessageDialog( _databaseConfigDialog,
                                  DialogFlags.Modal,
                                  MessageType.Info,
                                  ButtonsType.Ok, 
                                  "Database tables were created" );
            md.Run();
            md.Destroy();
         }
         catch (Exception createProblem)
         {
            _Trace( createProblem.ToString() );

            string msg = "Database tables were not created:\n"
               + createProblem.Message;

            MessageDialog ed = 
               new MessageDialog( _databaseConfigDialog,
                                  DialogFlags.Modal,
                                  MessageType.Error,
                                  ButtonsType.Ok, 
                                  msg );
            ed.Run();
            ed.Destroy();
         }
      }

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "ConfigWindow" );
      }

      bool _isOk = false;
      string _connectString;
      string _mp3RootDir;


      [Glade.Widget]
      Dialog _databaseConfigDialog;

      [Glade.Widget]
      Entry  _connectStringEntry;

      [Glade.Widget]
      Entry  _mp3RootDirEntry;

   }
}
