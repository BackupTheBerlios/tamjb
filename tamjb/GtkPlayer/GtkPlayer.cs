/// \file
/// $Id$
///
/// Glade.XML main window for the player
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
//    using System.Runtime.Remoting;
//    using System.Runtime.Remoting.Channels;
//    using System.Runtime.Remoting.Channels.Http;
//    using System.Runtime.Remoting.Channels.Tcp;
   using System.Threading;
   using Gtk;
   using GtkSharp;
   using Glade;

   using byteheaven.tamjb.Interfaces;

   ///
   /// A class to be connected to the Glade main window
   ///
   public class GtkPlayer
   {
      // Some constants
      readonly int TITLE_MAX_WIDTH = 60;
      readonly int ARTIST_MAX_WIDTH = 30;

      // What's comin' at cha, including the current track.
      // Wish this wasn't hard coded
      readonly int HISTORY_SIZE = 5;
      readonly int TRACK_LIST_SIZE = 11; // 5 old + 5 new + current

      // Temporary hardcoded table index for the suck metric
      readonly uint DOESNTSUCK = 0;

      enum TrackListOffset : int
      {
         ARTIST,
         TRACK_NAME,
         SUCK,
         MOOD,
         TRACK_INFO
      }

      ///
      /// Constructs the GtkPlayer from compiled-in Glade.XML resources
      ///
      public GtkPlayer()
      {
         //
         // With the null first parameter, this callw ill try to load
         // our resources as if they were compiled in. Which they are.
         // Right?
         //
         Glade.XML glade = new Glade.XML( null,
                                          "tam.GtkPlayer.exe.glade",
                                          "_mainWindow",
                                          null );

         glade.Autoconnect( this );
         _mainWindow = (Gtk.Window)glade.GetWidget( "_mainWindow" );
         Debug.Assert( null != _mainWindow );

         try
         {
            _settings = PlayerSettings.Fetch();
         }
         catch (Exception e)
         {
            _Status( "Note: Could not load settings, using defaults: " + e.Message,
                     35 );

            _settings = new PlayerSettings();
            _settings.serverName = "localhost";
            _settings.serverPort = 6543;
         }

         PlayerApp.connectionString = 
            "URI=file:/" + _settings.databaseFile;

         PlayerApp.mp3RootDir = _settings.mp3RootDir;

         PlayerApp.serverUrl =
            "tcp://" + _settings.serverName + ":" 
            + _settings.serverPort + "/Engine";

         _SetUpControls();

         // Load application icon here if possible, save for later use
         // in dialogs.
//          Gdk.Pixbuf icon = new Gdk.Pixbuf( Assembly.GetExecutingAssembly(), 
//                                            "appicon.png" );
//
//          Debug.Assert( null != icon );
//          _mainWindow.Icon = icon;
//          _configWindow.Icon = icon;
//          _whoAreYouWindow.Icon = icon;


         // Background processing callback
         Gtk.Timeout.Add( 2000, new Gtk.Function( _PollingCallback ) );
      }


      ///
      /// This is a wrapper for the Main.backend property that sets
      /// the status message appropriately while attempting to connect.
      ///
      public IEngine backend
      {
         ///
         /// Get a reference to the player backend
         ///
         get
         {
            try
            {
               return PlayerApp.backend;
            }
            catch ( System.Net.WebException snw )
            {
               // Probably couldn't connect. Use friendly message. Where is
               // that advanced error msg dialog I want?
               string msg = "Couldn't connect to the server '" 
                  + _settings.serverName + ":" 
                  + _settings.serverPort + "'";
               
               _Status( msg, 30 );
               
               // For now just rethrow.
               throw new ApplicationException( msg, snw );
            }
         }
      }

      ///
      /// A construction-time helper
      ///
      void _SetUpControls()
      {
         _Trace( "[_SetUpControls]" );

         _trackListStore = new ListStore( typeof(string),
                                          typeof(string),
                                          typeof(string),
                                          typeof(string),
                                          typeof(ITrackInfo) ); 

         //
         // To prevent constant resizing, insert empty entries into
         // the list and update them, ensuring that the list is always
         // the same size. :/
         // 

         for (int i = 0; i < TRACK_LIST_SIZE; i++)
            _trackListStore.AppendValues( "", "", "", "", null );

         
         TreeViewColumn column;
         // Should be a checkbox or something. Hmmm.
         column = new TreeViewColumn( "Suck", 
                                      new CellRendererText(),
                                      "text",
                                      TrackListOffset.SUCK );

         column.Sizing = TreeViewColumnSizing.GrowOnly; // Autosize, GrowOnly
         column.MinWidth = 25;
         // column.Toggled = new ToggledHandler( _OnSuckToggled );
         _trackListView.AppendColumn( column );

         // Should be a checkbox or something. Hmmm.
         column = new TreeViewColumn( "Mood", 
                                      new CellRendererText(),
                                      "text",
                                      TrackListOffset.MOOD );

         column.Sizing = TreeViewColumnSizing.GrowOnly; // Autosize, GrowOnly
         column.MinWidth = 25;
         // column.Toggled = new ToggledHandler( _OnSuckToggled );
         _trackListView.AppendColumn( column );

         column = new TreeViewColumn( "Artist", 
                                      new CellRendererText(),
                                      "text",
                                      TrackListOffset.ARTIST );
         column.Sizing = TreeViewColumnSizing.Fixed;
         column.MinWidth = 100;
         _trackListView.AppendColumn( column );

         column = new TreeViewColumn( "Track Name", 
                                      new CellRendererText(),
                                      "text",
                                      TrackListOffset.TRACK_NAME );
         column.Sizing = TreeViewColumnSizing.Fixed;
         column.MinWidth = 100;
         _trackListView.AppendColumn( column );

         _trackListView.Model = _trackListStore;

         // _trackListView.ColumnsAutosize();
         // _trackListView.HeadersClickable = true;
      }

      ///
      /// When the close button is clicked...
      ///
      void _OnDelete( object sender, DeleteEventArgs delArgs )
      {
         Application.Quit();
      }


      ///
      /// This is called periodically as a Timeout to check the status
      /// of the player and so on. 
      ///
      bool _PollingCallback()
      {
         _UpdateNow();

         if (PlayerApp.isStandalone) // not using a remote server?
         {
            try
            {
               PlayerApp.PollBackend(); // keep the engine working
            }
            catch (Exception e)
            {
               // dump stack trace
               _Trace( "Problem during Poll(): " + e.ToString() );
               _Status( e.Message, 60 );
            }

            try
            {
               PlayerApp.ScanForFiles();
            }
            catch (Exception e)
            {
               // dump stack trace
               _Trace( "Problem scanning for files: " + e.ToString() );
               _Status( "Problem scanning for files", 60 );
            }
         }

         if (_statusBarPopTimeout > 0)
         {
            -- _statusBarPopTimeout;
            if (_statusBarPopTimeout == 0)
               _statusBar.Pop( _statusId );
         }

         return true; // keep calling
      }

      ///
      /// Called from the PollingCallback, and also when we just changed
      /// the attributes of a track (so we KNOW we need an update)
      ///
      void _UpdateNow()
      {
         try
         {
            // State changed?
            if (backend.CheckState(ref _engineState) || _pendingUpdate )
            {
               _Status( "Updating...", 30 );
               backend.GetCurrentUserAndMood( ref _credentials, ref _mood );
               _UpdateNowPlayingInfo();
               _pendingUpdate = false;
         
               _Status( "Done", 4 );
            }
         }
         catch (Exception e)
         {
            ///
            /// \todo The GUI needs a status window with some sort of
            ////  "lost connection" indicator...
            ///

            // dump stack trace
            _Trace( "Could not update displayed track info: " 
                    + e.ToString() );

            _Status( "Could not update displayed track info: " 
                     + e.Message, 20 );
         }
      }
      
      ///
      /// Update the "now playing" display boxes and so on
      ///
      /// \throw Exception and friends. No attempt is made to catch them.
      ///
      void _UpdateNowPlayingInfo()
      {
         _Trace( "[_UpdateNowPlayingInfo]" );

         if (!_engineState.isPlaying)
         {
            _Status( "Server is stopped", 60 );
         }

         _UpdateTrackListView();
         _UpdateTransportButtonState();
      }


      ///
      /// Update the current track info display to match the current 
      /// selection in the listbox.
      ///
      void _UpdateTrackInfoDisplay()
      {
         _Trace( "[_UpdateTrackInfoDisplay]" );

         // Get selected track
         TreeModel model;
         TreeIter iter;
         _selectedTrackInfo = null;
         if (_trackListView.Selection.GetSelected( out model, out iter ))
         {
            _selectedTrackInfo =  (ITrackInfo)model.GetValue
               ( iter,
                 (int)TrackListOffset.TRACK_INFO );
         }
         
         if (null != _selectedTrackInfo)
         {
            // Retrieve track suck/mood details as necessary (or from
            // the model?)
            
            // Update display:
            _titleDisplay.Buffer.Text = _selectedTrackInfo.title;
            _artistDisplay.Buffer.Text = _selectedTrackInfo.artist;
            _albumDisplay.Buffer.Text = _selectedTrackInfo.album;
            _pathDisplay.Buffer.Text = _selectedTrackInfo.filePath;
         }
         else
         {
            _selectedTrackInfo = null;
            _titleDisplay.Buffer.Text = "";
            _artistDisplay.Buffer.Text = "";
            _albumDisplay.Buffer.Text = "";
            _pathDisplay.Buffer.Text = "";
         }

         _UpdateTrackInfoButtonState();
      }

      ///
      /// Sets the current playing track as the listbox selection
      /// The opposite of MatchTrackListToDisplay.
      ///
      void _SelectCurrentTrack()
      {
         _Trace( "[_SelectCurrentTrack]" );

         // First make sure the list's selected track is the
         // current track
         int selected = HISTORY_SIZE;
         _Trace( "  Selected: " + selected );
         if (selected >= 0)
         {
            TreePath path = new TreePath( selected.ToString() );
            _trackListView.Selection.SelectPath( path );
         }
      }         

      ///
      /// This is the opposite of _SelectCurrentTrack. It tries
      /// to find a track in the track list that matches what is 
      /// currently displayed, and highlights it.
      ///
      void _MatchTrackListToDisplay()
      {
         _Trace( "[_MatchTrackListToDisplay]" );

         Debug.Assert( null != _selectedTrackInfo, 
                       "No track is selected!" );

         // We are not locked to the current selection. Try to
         // select the currently-edited track in the list just to 
         // make the list look synced up with the edit window

         for (int row = 0; row < TRACK_LIST_SIZE; row++)
         {
            TreeIter rowIter;
            if (_trackListStore.IterNthChild( out rowIter, row ))
            {
               ITrackInfo rowInfo = (ITrackInfo)_trackListStore.GetValue
                  ( rowIter,
                    (int)TrackListOffset.TRACK_INFO );

               if (null == rowInfo) // empty row
                  continue;     // ** Skip **

               if (rowInfo.key == _selectedTrackInfo.key)
               {
                  // Same track! Select this row
                  _trackListView.SetCursor
                     ( _trackListStore.GetPath(rowIter),
                       _trackListView.Columns[0],
                       false );
                  
                  return;       // ** found it, return now **
               }
            }
         }

         // If we got here, not in list. Unselect all?
      }



      ///
      /// Get attributes for a particular track using the current
      /// logged in user's credentials
      ///
      void _GetAttributes( uint trackKey,
                           out double suckPercent,
                           out double moodPercent )
      {
         // If we're not connected OR we don't know who we are, just
         // don't worry about it.
         if (null == backend ||
             null == _credentials || null == _mood)
         {
            suckPercent = 0.0;
            moodPercent = 100.0;
            return;
         }

         backend.GetAttributes( _credentials,
                                 _mood,
                                 trackKey,
                                 out suckPercent,
                                 out moodPercent );

         suckPercent /= 100;
         moodPercent /= 100;
      }

      ///
      /// Update the track list display window. Dude!
      ///
      void _UpdateTrackListView()
      {
         int i = _engineState.currentTrackIndex - HISTORY_SIZE;
         int row = 0;
         if (i < 0)
         {
            // Start at an offset in the display listbox!
            row = -i;
            i = 0;
         }

         TreeIter iter;
         for (/* everything initialized already */;
              i < _engineState.playQueue.Length;
              i++, row++ )
         {
            ITrackInfo info = _engineState.playQueue[i];

            if (null == info)   // Other threads may have removed the entry.
               break;

            if (row >= TRACK_LIST_SIZE) // More in queue than we can display
               break;

            if (_trackListStore.IterNthChild( out iter, row ))
            {
               double suckLevel;
               double moodLevel;
               _GetAttributes( info.key, out suckLevel, out moodLevel );

               _trackListStore.SetValue( iter, 
                                         (int)TrackListOffset.ARTIST, 
                                         info.artist );
               _trackListStore.SetValue( iter, 
                                         (int)(int)TrackListOffset.TRACK_NAME, 
                                         _FixTitle(info) );
               _trackListStore.SetValue( iter, 
                                         (int)TrackListOffset.SUCK, 
                                         suckLevel.ToString( "f0" ) );

               _trackListStore.SetValue( iter, 
                                         (int)TrackListOffset.MOOD,
                                         moodLevel.ToString( "f0" ) );

               _trackListStore.SetValue( iter, 
                                         (int)TrackListOffset.TRACK_INFO,
                                         info );
            }
         }

         while (row < TRACK_LIST_SIZE)
         {
            if (_trackListStore.IterNthChild( out iter, row ))
            {
               _trackListStore.SetValue( iter, (int)TrackListOffset.ARTIST, "" );
               _trackListStore.SetValue( iter, (int)TrackListOffset.TRACK_NAME, "" );
               _trackListStore.SetValue( iter, (int)TrackListOffset.SUCK, "" );
               _trackListStore.SetValue( iter, (int)TrackListOffset.MOOD, "" );
               _trackListStore.SetValue( iter, (int)TrackListOffset.TRACK_INFO, null );
            }
            ++row;
         }

         if (_nowPlayingCheck.Active)
         {
            _SelectCurrentTrack();
            _UpdateTrackInfoDisplay();
         }
         else
         {
            _MatchTrackListToDisplay();
         }
      }


      ///
      /// Gets either the title or (if that is empty or all spaces or what)
      /// the filename from info
      ///
      string _FixTitle( ITrackInfo info )
      {
         ///
         /// \todo check for all-whitespace title here
         ///
         if (info.title.Length > 0)
         {
            return info.title;
         }
         else
         {
            string title = info.filePath;
            if (title.Length > TITLE_MAX_WIDTH)
            {
               title = "..." 
                  + title.Substring( title.Length - (TITLE_MAX_WIDTH - 4) );
            }
            
            return title;
         }
      }


      ///
      /// Called when the window is deleted via the window manager
      ///
      void _DeleteHandler( object sender, DeleteEventArgs delArgs )
      {
         _Trace( "[_DeleteHandler]" );
         Application.Quit();
      }

      void _SuckBtnClick( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_SuckBtnClick]" );

            if (_engineState.currentTrackIndex < 0)
               return;

            if (null == _selectedTrackInfo) 
            {
               _Trace( "  No track selected" );
               return;
            }

            // Whether the suck attribute table is active or not, 
            // send feedback to the suck table.

            uint suckTrackKey = _selectedTrackInfo.key;

            // Stop playing this track if it is the current track
            if (_engineState.currentTrack.key == suckTrackKey)
               backend.GotoNextFile( _credentials, suckTrackKey );

            // Flag the previously playing track as suck
            backend.IncreaseSuckZenoStyle( _credentials, suckTrackKey );

            _SetPendingUpdate(); // update state to match backend
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _RuleBtnClick( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_RuleBtnClick]" );

            if (_engineState.currentTrackIndex < 0)
               return;

            if (null == _selectedTrackInfo) 
            {
               _Trace( "  No track selected" );
               return;
            }

            // Whether the suck attribute table is active or not, 
            // send feedback to the suck table.

            uint trackKey = _selectedTrackInfo.key;

            // Flag the previously playing track as suck
            backend.DecreaseSuckZenoStyle( _credentials, trackKey );

            _SetPendingUpdate(); // update state to match backend
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Called when the user things this song is wrong for the playlist.
      ///
       void _MoodNoBtnClick( object sender, EventArgs args )
      {
         _Trace( "[_MoodNoBtnClick]" );

         try
         {
            if (null == _selectedTrackInfo) 
            {
               _Trace( "  No track selected" );
               return;
            }
               
            // Send the player to the next file first.
            if (_engineState.currentTrack.key == _selectedTrackInfo.key)
               backend.GotoNextFile( _credentials, _selectedTrackInfo.key );

            // Now that it's no longer playing, update the track.
            backend.DecreaseAppropriateZenoStyle( _credentials, 
                                                   _mood,
                                                   _selectedTrackInfo.key );

            _SetPendingUpdate(); // update to match backend
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Called when the user things this song is wrong for the playlist.
      ///
      void _MoodYesBtnClick( object sender, EventArgs args )
      {
         _Trace( "[_MoodYesBtnClick]" );

         try
         {
            if (null == _selectedTrackInfo) 
            {
               _Trace( "  No track selected" );
               return;
            }
               
            // Now that it's no longer playing, update the track.
            backend.IncreaseAppropriateZenoStyle( _credentials, 
                                                   _mood,
                                                   _selectedTrackInfo.key );

            _SetPendingUpdate(); // update to match backend
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _NextBtnClick( object sender, EventArgs args )
      {
         _Trace( "[_NextBtnClick]" );
         try
         {
            if (null == _selectedTrackInfo) 
            {
               _Trace( "  No track selected" );
               return;
            }
               
            backend.GotoNextFile( _credentials, _selectedTrackInfo.key );
            _SetPendingUpdate();
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Go back to the previous song. 
      ///
      void _PrevBtnClick( object sender, EventArgs args )
      {
         _Trace( "[_PrevBtnClick]" );

         try
         {
            
            if (_engineState.currentTrackIndex <= 0)
            {
               return;
            }

            if (null == _selectedTrackInfo) 
            {
               _Trace( "  No track selected" );
               return;
            }

            backend.GotoPrevFile( _credentials, _selectedTrackInfo.key );
            _UpdateTransportButtonState();
            _SetPendingUpdate(); // Track is changing...
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Callback for the stop button. Tries to make the backend stop
      /// playing.
      ///
      void _OnStopBtnClicked( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnStopBtnClicked]" );

            backend.StopPlaying(); // Please stop! Please stop!
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Callback for the play button
      ///
      void _OnPlayBtnClicked( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnPlayBtnClicked]" );

            backend.StartPlaying(); // (if not already started)
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Run the configuration dialog.
      ///
      void _ConfigBtnClick( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_ConfigBtnClick]" );

            if (PlayerApp.isStandalone)
            {
               LocalConfigDialog config = 
                  new LocalConfigDialog( _mainWindow,
                                         _settings.databaseFile,
                                         _settings.mp3RootDir );
               config.Run();
               if (config.isOk)
               {
                  _settings.databaseFile = config.database;
                  _settings.mp3RootDir = config.mp3RootDir;

                  // Connection string for sqlite
                  PlayerApp.connectionString = 
                     "URI=file:/" + config.database;

                  PlayerApp.mp3RootDir = config.mp3RootDir;

                  _settings.Store();

                  if (config.needToCreateDatabase)
                  {
                     try
                     {
                        PlayerApp.CreateDatabase( PlayerApp.connectionString );
                     }
                     catch (Exception createProblem)
                     {
                        _Complain( "Could not create database",
                                   createProblem );
                     }
                  }
               }
            }
            else
            {
               ConfigDialog config = 
                  new ConfigDialog( _mainWindow,
                                    _settings.serverName,
                                    _settings.serverPort.ToString() );

               config.Run();
               if (config.isOk)
               {
                  _settings.serverName = config.serverName;
                  _settings.serverPort = config.serverPort;
                  
                  //
                  // Set the new server url
                  //
                  PlayerApp.serverUrl =
                     "tcp://" + _settings.serverName + ":" 
                     + _settings.serverPort + "/Engine";
                  
                  // If we got here, nothing threw an exception. Wow!
                  // Save for future generations!
                  _settings.Store();
               }
            }
         }
         catch (Exception e)
         {
            _Complain( "Something went wrong configuring or saving settings", 
                       e );
         }
      }

      ///
      /// Run the configuration dialog.
      ///
      void _AudioBtnClick( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_AudioBtnClick]" );

            if (null != backend)
            {
               MiscSettingsDialog dlg = 
                  new MiscSettingsDialog( _mainWindow, backend );

               dlg.Run();
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Show the "who the heck are you"? Dialog
      ///
      void _UserBtnClick( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_UserBtnClick]" );

            MoodDialog moodWin = 
               new MoodDialog( _mainWindow,
                               backend,
                               _credentials,
                               _mood,
                               MoodDialog.DefaultField.USER );

            moodWin.Run();
            if (moodWin.isOk)
            {
               // get new/existing user's credentials from backend
               _credentials = backend.GetUser( moodWin.userName );
               if (null == _credentials)
                  _credentials = backend.CreateUser( moodWin.userName );

               backend.RenewLogon( _credentials );

               if ("" == moodWin.moodName)
               {
                  _mood = null;
               }
               else
               {
                  // Find the mood that matches this name
                  _mood = backend.GetMood( _credentials, 
                                            moodWin.moodName );

                  if (null == _mood)
                  {
                     _mood = backend.CreateMood( _credentials, 
                                                  moodWin.moodName );
                  }

                  backend.SetMood( _credentials, _mood );
               }
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Show the "who the heck are you" dialog but with mood selected
      ///
      void _MoodBtnClick( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_MoodBtnClick]" );

            MoodDialog moodWin = 
               new MoodDialog( _mainWindow,
                               backend,
                               _credentials,
                               _mood,
                               MoodDialog.DefaultField.MOOD );

            moodWin.Run();
            if (moodWin.isOk)
            {
               // get new/existing user's credentials from backend
               _credentials = backend.GetUser( moodWin.userName );
               if (null == _credentials)
                  _credentials = backend.CreateUser( moodWin.userName );

               backend.RenewLogon( _credentials );

               if ("" == moodWin.moodName)
               {
                  _mood = null;
               }
               else
               {
                  // Find the mood that matches this name
                  _mood = backend.GetMood( _credentials, 
                                            moodWin.moodName );

                  if (null == _mood)
                  {
                     _mood = backend.CreateMood( _credentials, 
                                                  moodWin.moodName );
                  }

                  backend.SetMood( _credentials, _mood );
               }
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }
      
      
      void _OnLockToggled( object sender, EventArgs args )
      {
         /// \todo Have the appropriate toggles share 1 function.
         ///
         _Trace( "[_OnLockToggled]" );
         try
         {
            // Jump immediately to the current track, if enabled
            if (_nowPlayingCheck.Active)
            {
               _SelectCurrentTrack();
               _UpdateTrackInfoDisplay();
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _OnListCursorChanged( object sender, EventArgs e )
      {
         _Trace( "[_OnListCursorChanged]" );
         
         try
         {
            if (_nowPlayingCheck.Active)
            {
               _SelectCurrentTrack(); // restore to current track
            }
            else
            {
               _UpdateTrackInfoDisplay(); 
            }
         }
         catch (Exception ex)
         {
            _Trace( e.ToString() );
         }
      }


      void _SetPendingUpdate()
      {
         _pendingUpdate = true;

         _UpdateNow();
      }

      ///
      /// Here we decide which controls are greyed out, etc, based
      /// on the current state.
      ///
      void _UpdateTransportButtonState()
      {
         _Trace( "[_UpdateTransportButtonState]" );

         bool isConnected = (null != backend);

         _nextBtn.Sensitive = isConnected;
         _prevBtn.Sensitive = isConnected;

         if (!isConnected || !_engineState.isPlaying)
         {
            _stopBtn.Sensitive = false;
            _playBtn.Sensitive = true;
         }
         else
         {
            _stopBtn.Sensitive = true;
            _playBtn.Sensitive = false;
         }

         if (isConnected)
         {
            if (_engineState.currentTrackIndex <= 0)
               _prevBtn.Sensitive = false;
            else
               _prevBtn.Sensitive = true;
         }
      }

      void _UpdateTrackInfoButtonState()
      {
         _Trace( "[_UpdateTrackInfoButtonState]" );

         if (null == _credentials)
            _userBtn.Label = "(nobody)";
         else
            _userBtn.Label = _credentials.name;

         if (null == _mood)
            _moodBtn.Label = "(neutral)";
         else
            _moodBtn.Label = _mood.name;

         if (null == _selectedTrackInfo)
         {
            _suckBtn.Sensitive = false;
            _ruleBtn.Sensitive = false;
            _moodNoBtn.Sensitive = false;
            _moodYesBtn.Sensitive = false;
         }
         else
         {
            _suckBtn.Sensitive = true;
            _ruleBtn.Sensitive = true;
            _moodNoBtn.Sensitive = true;
            _moodYesBtn.Sensitive = true;
         }
      }

      void _Status( string msg, int timeout )
      {
         _Trace( "Status: " + msg ); // to console also

         _statusBar.Pop( _statusId );
         _statusBar.Push( _statusId, msg );
         _statusBarPopTimeout = timeout / 2; // 2 second poll interval
      }

      ///
      /// A function to call when you want the user to know how
      /// annoyed you are with the failings of some third party 
      /// software, or of his input.
      ///
      /// \param msg Describes generally what went you were trying to
      ///   do that failed ("Could not create file")
      /// \param e The exception you got.
      ///
      /// \todo Implement this as something nicer than a stupid 
      ///   MessageBox
      ///
      void _Complain( string msg, Exception e )
      {
         // Dump details to log for posterity
         _Trace( msg );
         _Trace( e.ToString() );

         MessageDialog md = 
            new MessageDialog( _mainWindow, 
                               DialogFlags.Modal,
                               MessageType.Error,
                               ButtonsType.Ok, 
                               msg );
         md.Run();
         md.Destroy();
      }

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "GtkPlayer" );
      }

      // Configuration

      PlayerSettings _settings;

      // Engine remote connection

      EngineState _engineState = new EngineState();

      // State for the list/tree widgets

      ListStore _trackListStore;

      // State for the configurable attributes. I guess.

      // For now, the user credentials is a uint placeholder. Temporary.
      Credentials _credentials = new Credentials();
      Mood        _mood = new Mood();

      // Holds basic info about the current track.
      ITrackInfo   _selectedTrackInfo = null;

      // Misc

      bool _pendingUpdate = true;

      //
      // -- Widgets that are attached to Glade
      //
      [Glade.Widget]
      TreeView _trackListView;

      [Glade.Widget]
      TextView _titleDisplay;

      [Glade.Widget]
      TextView _artistDisplay;

      [Glade.Widget]
      TextView _pathDisplay;

      [Glade.Widget]
      TextView _albumDisplay;

      [Glade.Widget]
      Button _userBtn;

      [Glade.Widget]
      Button _moodBtn;

      [Glade.Widget]
      Button _suckBtn;

      [Glade.Widget]
      Button _ruleBtn;

      [Glade.Widget]
      Button _moodNoBtn;

      [Glade.Widget]
      Button _moodYesBtn;

      [Glade.Widget]
      CheckButton _nowPlayingCheck;

      [Glade.Widget]
      Button _prevBtn;

      [Glade.Widget]
      Button _nextBtn;

      [Glade.Widget]
      Button _stopBtn;

      [Glade.Widget]
      Button _playBtn;
      
      [Glade.Widget]
      Statusbar _statusBar;

      // Application windows
      Window _mainWindow;


      int _statusBarPopTimeout = 0;
      uint _statusId = 1;        // unique id of the gtk player's status msgs
   }

}
