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
   /// A class to be connected to the Glade main window
   ///
   public class LocalConfigDialog
   {
      public bool isOk
      {
         get
         {
            return _isOk;
         }
      }

      public string database
      {
         get
         {
            return _database;
         }
      }

      public string mp3RootDir
      {
         get
         {
            return _mp3RootDir;
         }
      }

      public bool needToCreateDatabase
      {
         get
         {
            return _needToCreate;
         }
      }

      ///
      /// Creates the mood window dialog but does not display it. 
      /// (use Run()).
      ///
      /// cred and mood are directly modified on successful validation.
      ///
      public LocalConfigDialog( Gtk.Window        parent,
                                string            database,
                                string            rootDir )
      {
         Glade.XML glade = new Glade.XML( null,
                                          "tam.GtkPlayer.exe.glade",
                                          "_localConfigDialog",
                                          null );

         glade.Autoconnect( this );
         
         Debug.Assert( null != _localConfigDialog );

         _localConfigDialog.TransientFor = parent;

         _databaseEntry.Text = database;
         _mp3RootDirEntry.Text = rootDir;
      }

      public void Run()
      {
         _localConfigDialog.Run();
      }

      void _OnUserResponse( object sender, ResponseArgs args )
      {
         _Trace( "[_OnUserResponse]" );

         switch (args.ResponseId)
         {
         case ResponseType.Ok:
            try
            {
               _database = _databaseEntry.Text;
               _mp3RootDir = _mp3RootDirEntry.Text;

               // Complain if the mp3 root dir doesn't exist.
               DirectoryInfo mp3DirInfo = new DirectoryInfo( _mp3RootDir );
               if (!mp3DirInfo.Exists)
               {
                  MessageDialog md = 
                     new MessageDialog( _localConfigDialog, 
                                        DialogFlags.Modal,
                                        MessageType.Error,
                                        ButtonsType.Ok, 
                                        ("Directory '" + _mp3RootDir 
                                         + "' does not exist") );
                  md.Run();
                  md.Destroy();
                  return;
               }

               // If the db file does not exist, create it. If necessary.
               FileInfo dbInfo = new FileInfo( _database );
               if (!dbInfo.Exists)
               {
                  // If we don't reate the file, quick exit here (keep
                  // the dialog going)
                  if (!_CreateDatabasePrompt( _database ))
                     return;    
               }

               // If we got here, the controls validated
               _localConfigDialog.Destroy();
               _isOk = true;
            }
            catch (Exception exception)
            {
               _Trace( exception.ToString() );
            }
            break;
            
         case ResponseType.Cancel:
         default:
            _localConfigDialog.Destroy();
            break;              // er
         }
      }

      bool _CreateDatabasePrompt( string filename )
      {
         string msg = "'" + filename + "' does not exist.\n"
            + "Create a new database?";

         // Confirm that this is really what we want (should not
         // be hardcoded in English)
         MessageDialog md = 
            new MessageDialog( _localConfigDialog, 
                               DialogFlags.Modal,
                               MessageType.Question,
                               ButtonsType.YesNo, 
                               msg );
  
         ResponseType result = (ResponseType)md.Run();
         md.Destroy();

         if (result != ResponseType.Yes)
            return false;

         _needToCreate = true;
         return true;           // it's OK
      }
                  

      void _OnDatabaseBtnClick( object sender, EventArgs args )
      {
         FileSelection dlg = new FileSelection( "Enter Database Path" );
         dlg.Filename = _databaseEntry.Text;
         if ((ResponseType)dlg.Run() == ResponseType.Ok)
            _databaseEntry.Text = dlg.Filename;

         dlg.Destroy();
      }

      void _OnMp3BtnClick( object sender, EventArgs args )
      {
         // The GTK widget is not well suited to selecting directories
         // right now. I'll do the best I can for the moment:
         FileSelection dlg = new FileSelection( "Enter Database Path" );
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

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "ConfigWindow" );
      }

      bool _isOk = false;
      string _database;
      string _mp3RootDir;

      // if true, we will attempt to creat the database file
      bool _needToCreate = false; 

      [Glade.Widget]
      Dialog _localConfigDialog;

      [Glade.Widget]
      Entry  _databaseEntry;

      [Glade.Widget]
      Entry  _mp3RootDirEntry;

   }
}
