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
                                          "GtkPlayer",
                                          null );

         glade.Autoconnect( this );

         try
         {
            _settings = PlayerSettings.Fetch();
         }
         catch (Exception e)
         {
            _Status( "Could not load settings, using defaults: " + e.Message,
                     15 );

            _settings = new PlayerSettings();
            _settings.serverName = "localhost";
            _settings.serverPort = 6543;
         }

         _SetUpControls();

         _configDlg = new ConfigDlg( _settings );

         // Background processing callback
         Gtk.Timeout.Add( 2000, new Gtk.Function( _PollingCallback ) );
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

         column.Sizing = TreeViewColumnSizing.Autosize; // Autosize, GrowOnly
         column.MinWidth = 25;
         // column.Toggled = new ToggledHandler( _OnSuckToggled );
         _trackListView.AppendColumn( column );

         // Should be a checkbox or something. Hmmm.
         column = new TreeViewColumn( "Mood", 
                                      new CellRendererText(),
                                      "text",
                                      TrackListOffset.MOOD );

         column.Sizing = TreeViewColumnSizing.Autosize; // Autosize, GrowOnly
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
            if (null == _backend)
            {
               // Try now: throws on failure.
               Debug.Assert( null != _settings, "settings object missing" );
               _Status( "Checking...", 2 );
               _backend = _ConnectToEngine( _settings.serverName,
                                            _settings.serverPort );
               _Status( "Done", 4 );
            }

            // State changed?
            if (_backend.CheckState(ref _engineState) || _pendingUpdate )
            {
               _Status( "Updating...", 30 );
               _UpdateNowPlayingInfo();
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

         if (null != _engineState)
         {
            if (_engineState.currentTrackIndex < 0)
            {
               _Status( "Server is stopped", 30 );
            }
         }
         _pendingUpdate = false;
         
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

         if (null == _engineState) // Hrmm
            return;

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

         if (null == _engineState) // Hrmm
            return;

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
         // HACK
         suckPercent = 50.0;
         moodPercent = 49.1;
      }

      ///
      /// Update the track list display window. Dude!
      ///
      void _UpdateTrackListView()
      {
         if (null == _engineState) // huh
            return;

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
              i < _engineState.Count;
              i++, row++ )
         {
            ITrackInfo info = _engineState[i];

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
                                         suckLevel.ToString( "f1" ) );

               _trackListStore.SetValue( iter, 
                                         (int)TrackListOffset.MOOD,
                                         moodLevel.ToString( "f1" ) );

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
               _backend.GotoNextFile( _credentials, suckTrackKey );

            // Flag the previously playing track as suck
            _backend.IncreaseSuckZenoStyle( _credentials, suckTrackKey );

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
            _backend.DecreaseSuckZenoStyle( _credentials, trackKey );

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
               _backend.GotoNextFile( _credentials, _selectedTrackInfo.key );

            // Now that it's no longer playing, update the track.
            _backend.DecreaseAppropriateZenoStyle( _credentials, 
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
            _backend.IncreaseAppropriateZenoStyle( _credentials, 
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
            if (null == _engineState)
               return;

            if (null == _selectedTrackInfo) 
            {
               _Trace( "  No track selected" );
               return;
            }
               
            _backend.GotoNextFile( _credentials, _selectedTrackInfo.key );
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
            
            if ((null == _engineState) ||
                (_engineState.currentTrackIndex <= 0))
            {
               return;
            }

            if (null == _selectedTrackInfo) 
            {
               _Trace( "  No track selected" );
               return;
            }

            _backend.GotoPrevFile( _credentials, _selectedTrackInfo.key );
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

            if (null == _engineState)
               return;

            _backend.StopPlaying(); // Please stop! Please stop!
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

            if (null == _engineState)
               return;

            _backend.StartPlaying(); // (if not already started)
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }


      IEngine _ConnectToEngine( string serverName,
                                int serverPort )
      {
         _Trace( "_ConnectToEngine" );
         try
         {
            //
            // http: and tcp: both valid here, although http seems
            // to be broken in this release (beta1) of mono
            //
            string serverUrl = 
               "http://" + serverName + ":" + serverPort + "/Engine";

            _Status( serverUrl + " - Trying...", 10 );

            // Retrieve a reference to the remote object
            IEngine engine = (IEngine) Activator.GetObject( typeof(IEngine), 
                                                            serverUrl );

            _Status( serverUrl + " - Connected", 10 );

            _credentials = engine.GetDefaultCredentials();
            _mood = engine.GetDefaultMood();
            return engine;
         }
         catch ( System.Net.WebException snw )
         {
            // Probably couldn't connect. Use friendly message. Where is
            // that advanced error msg dialog I want?
            string msg = "Couldn't connect to the server '" 
               + serverName + ":" + serverPort + "'";

            _Status( msg, 10 );

            // For now just rethrow. (Ick!)
            throw new ApplicationException( msg, snw );
         }
      } 

      ///
      /// Run the configuration dialog.
      ///
      void _ConfigBtnClick( object sender, EventArgs args )
      {
         try
         {
            _Trace( "Config" );

            if ((int)ResponseType.Ok == _configDlg.Run())
            {
               _Trace( "OK" );

               // (re)connect to the player
               _backend = null;
               _backend = _ConnectToEngine( _settings.serverName,
                                            _settings.serverPort );

               // Save for future generations!
               _settings.Store();
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

         bool isConnected = (null != _engineState);

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

         _userBtn.Label = "User: " + _credentials.name;
         _moodBtn.Label = "Mood: " + _mood.name;

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

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "GtkPlayer" );
      }

      void _Status( string msg, int timeout )
      {
         _Trace( "Status: " + msg ); // to console also

         _statusBar.Pop( _statusId );
         _statusBar.Push( _statusId, msg );
         _statusBarPopTimeout = timeout / 2; // 2 second poll interval
      }

      // Configuration

      PlayerSettings _settings;
      ConfigDlg      _configDlg;

      // Engine remote connection

      IEngine _backend = null;
      IEngineState _engineState = null;

      // State for the list/tree widgets

      ListStore _trackListStore;

      // State for the configurable attributes. I guess.

      // For now, the user credentials is a uint placeholder. Temporary.
      ICredentials _credentials = null;
      IMood        _mood = null;

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

      int _statusBarPopTimeout = 0;
      uint _statusId = 1;        // unique id of the gtk player's status msgs
   }

}
