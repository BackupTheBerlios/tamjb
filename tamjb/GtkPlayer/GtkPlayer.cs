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

namespace tam.GtkPlayer
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
      readonly int QUEUE_MIN_SIZE = 6; 

      // Wish this also wasn't hard coded
      readonly int HISTORY_VISIBLE_COUNT = 5;

      // Temporary hardcoded table index for the suck metric
      readonly uint DOESNTSUCK = 0;

      ///
      /// Constructs the GtkPlayer from compiled-in Glade.XML resources
      ///
      public GtkPlayer()
      {
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
            _settings.serverPort = 5432;
         }

         ///
         /// \todo Retrieve the glade resources from our assembly.
         ///
         Glade.XML glade = new Glade.XML( null,
                                          "tam.GtkPlayer.exe.glade",
                                          "GtkPlayer",
                                          null );

         glade.Autoconnect( this );

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

         _historyListStore = new ListStore( typeof(string),
                                            typeof(string),
                                            typeof(ITrackInfo) ); 

         //
         // To prevent constant resizing, insert empty entries into
         // the list and update them, ensuring that the list is always
         // the same size. :/
         // 
         for (int i = 0; i < HISTORY_VISIBLE_COUNT; i++)
            _historyListStore.AppendValues( "", "", null );

         _historyListView.AppendColumn( "Artist", 
                                        new CellRendererText(),
                                        "text",
                                        0 );

         _historyListView.AppendColumn( "Track Name", 
                                        new CellRendererText(),
                                        "text",
                                        1 );

         _historyListView.Model = _historyListStore;


         //
         // Set up the "future" history
         //
         _futureListStore = new ListStore( typeof(string),
                                           typeof(string),
                                           typeof(bool) ); 

         // Create rows to store data
         for (int i = 0; i < (QUEUE_MIN_SIZE - 1); i++)
            _futureListStore.AppendValues( "", "", false );


         TreeViewColumn column;
         column = new TreeViewColumn( "Artist", 
                                      new CellRendererText(),
                                      "text",
                                      0 );
         column.Sizing = TreeViewColumnSizing.GrowOnly;
         _futureListView.AppendColumn( column );

         column = new TreeViewColumn( "Track Name", 
                                      new CellRendererText(),
                                      "text",
                                      1 );
         column.Sizing = TreeViewColumnSizing.GrowOnly;
         _futureListView.AppendColumn( column );

         // Should be a checkbox or something. Hmmm.
         column = new TreeViewColumn( "Suck", 
                                      new CellRendererToggle(),
                                      "active",
                                      2 );
         column.Sizing = TreeViewColumnSizing.Fixed; // Autosize, GrowOnly
         // column.Toggled = new ToggledHandler( _OnSuckToggled );
         _futureListView.AppendColumn( column );

         _futureListView.Model = _futureListStore;

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
         _Trace( "_UpdateNowPlayingInfo" );

         ITrackInfo trackInfo = null;
         if (null != _engineState)
            trackInfo = _engineState.currentTrack;

         if (null == trackInfo) // not playing after all
         {
            _Status( "Server is stopped", 30 );
            _titleDisplay.Buffer.Text = "(stopped)";
            _artistDisplay.Buffer.Text = "";
         }
         else
         {
            _pendingUpdate = false;

            uint doesntSuckLevel = _backend.GetAttribute( DOESNTSUCK, 
                                                          trackInfo.key );

            // Invert the doesntSuck value to create a suck metric
            double suckPercent = (10000 - doesntSuckLevel) / 100.0;


            _titleDisplay.Buffer.Text = trackInfo.title;
            _artistDisplay.Buffer.Text = trackInfo.artist;
            _albumDisplay.Buffer.Text = trackInfo.album;
            _pathDisplay.Buffer.Text = trackInfo.filePath;
            _suckSlider.Value = suckPercent;
            _suckValue = suckPercent;

            // Ensure that the end of the path is visible? Maybe later
//             TextIter iter = 
//                _pathDisplay.GetIterAtLocation( trackInfo.filePath.Length,
//                                                1 );
//             _pathDisplay.ScrollToIter( iter,
//                                        0.0, // within_margin
//                                        true,
//                                        1.0, // xalign
//                                        0.0  ); // yalign 

            //
            // Should we dynamically build buttons here? Or hide/unhide
            // some prebuild controls?
            //
            _isAppropriateActive = false;
            _isSuckActive = false;
            foreach (uint key in _engineState.activeCriteria)
            {
               _Trace( "Active: " + key );
               if (0 == key)
               {
                  _isSuckActive = true;
               }
               else if (_appropriateKey == key)
               {
                  _isAppropriateActive = true;

                  double appropriateLevel = 
                     (double)_backend.GetAttribute( (uint)_appropriateKey,
                                                    trackInfo.key );

                  appropriateLevel /= 100.0;
                  _appropriateSlider.Value = appropriateLevel;
                  _appropriateValue = appropriateLevel;
               }
            }
         }
         
         _UpdateHistoryView();
         _UpdateFutureView();
         _UpdateButtonState();
      }
      
      void _UpdateHistoryView()
      {
         if (null == _engineState) // huh
            return;

         // Only show the last HISTORY_VISIBLE_COUNT entries

         int row = HISTORY_VISIBLE_COUNT - 1;
         TreeIter iter;
         for (int i = (_engineState.currentTrackIndex - 1); i >= 0; i--, row--)
         {
            ITrackInfo info = _engineState[i];

            if (null == info)   // Other threads may have removed the entry.
               break;

            if (row >= 0 &&
                _historyListStore.IterNthChild( out iter, row ))
            {
               _historyListStore.SetValue( iter, 0, info.artist );
               _historyListStore.SetValue( iter, 1, _FixTitle(info) );
            }
         }

         while (row >= 0)
         {
            if (_historyListStore.IterNthChild( out iter, row ))
            {
               _historyListStore.SetValue( iter, 0, "" );
               _historyListStore.SetValue( iter, 1, "" );
            }
            --row;
         }
      }

      void _UpdateFutureView()
      {
         if (null == _engineState) // No state!
            return;

         TreeIter iter;
         int row = 0;           // row in list
         for (int i = _engineState.currentTrackIndex + 1; 
              i < _engineState.Count; 
              i++, row++ )
         {
            ITrackInfo info = _engineState[i];

            if (null == info)   // Other threads may have removed the entry.
               break;

            if (_futureListStore.IterNthChild( out iter, row ))
            {
               _futureListStore.SetValue( iter, 0, info.artist );
               _futureListStore.SetValue( iter, 1, _FixTitle(info) );
               _futureListStore.SetValue( iter, 2, false ); // doesn't suck yet
            }
         }

         while (row < QUEUE_MIN_SIZE)
         {
            if (_futureListStore.IterNthChild( out iter, row ))
            {
               _futureListStore.SetValue( iter, 0, "" );
               _futureListStore.SetValue( iter, 1, "" );
            }
            ++row;
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

      void _OnSuckSliderChanged( object sender, EventArgs e )
      {
         _Trace( "[_OnSuckSliderChanged]" );
            
         try
         {
            if (!_isSuckActive)
            {
               // _Trace( "Slider changed, but attribute is not active!" );
            }
            else
            {
               _OnSliderChanged( _suckSlider,
                                 ref _suckValue,
                                 DOESNTSUCK,
                                 true ); // inverted
            }
         }
         catch (Exception ex)
         {
            _Trace( ex.ToString() );
         }
      }

      void _OnAppropriateSliderChanged( object sender, EventArgs e )
      {
         _Trace( "[_OnAppropriateSliderChanged]" );
         try
         {
            if (!_isAppropriateActive)
            {
               // _Trace( "Slider changed, but attribute is not active!" );
            }
            else
            {
               _OnSliderChanged( _appropriateSlider,
                                 ref _appropriateValue,
                                 (uint)_appropriateKey,
                                 false ); // not inverted
            }
         }
         catch (Exception ex)
         {
            _Trace( ex.ToString() );
         }
      }

      ///
      /// Helper function to handle the change of any slider
      ///
      void _OnSliderChanged( Scale slider,
                             ref double cachedValue,
                             uint attributeKey,
                             bool isInverted )
      {
         if (_engineState.currentTrackIndex < 0)
            return;

         // avoid updating database if it still has the same value
         double newValue = slider.Value;
         if (newValue == cachedValue) 
            return;

         cachedValue = newValue;
         uint trackKey = _engineState.currentTrack.key;

         newValue = newValue * 100.0;

         if (isInverted)        // Is less better?
            newValue = 10000.0 - newValue;

         // Paranoia is good
         if (newValue < 0.0)
            newValue = 0.0;
         
         if (newValue > 10000.0)
            newValue = 10000.0;
                  
         // Flag the previously playing track as suck
         _backend.SetAttribute( attributeKey, trackKey, (uint)newValue );

         _SetPendingUpdate(); // Call after updating backend
      }

      void _SuckBtnClick( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_SuckBtnClick]" );

            if (_engineState.currentTrackIndex >= 0)
            {
               // Whether the suck attribute table is active or not, 
               // send feedback to the suck table.

               uint suckTrackKey = _engineState.currentTrack.key;

               // Stop playing this track NOW
               _backend.GotoNextFile(); // this takes a while

               // Flag the previously playing track as suck
               _backend.DecreaseAttributeZenoStyle( DOESNTSUCK, suckTrackKey );

               _SetPendingUpdate(); // update state to match backend
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Called when the user things this song is wrong for the playlist.
      ///
      void _WrongBtnClick( object sender, EventArgs args )
      {
         _Trace( "_WrongBtnClick" );

         try
         {
            // No "appropriate button" should be active?
            if (! _isAppropriateActive)
            {
               _Trace( "Click while not active?" );
               return;
            }

            ITrackInfo info = _engineState.currentTrack;
               
            // Send the player to the next file first.
            _backend.GotoNextFile();

            // Now that it's no longer playing, update the track.
            _backend.DecreaseAttributeZenoStyle( _appropriateKey, 
                                                 info.key );

            _SetPendingUpdate(); // update to match backend
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _NextBtnClick( object sender, EventArgs args )
      {
         try
         {
            _backend.GotoNextFile();
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
         try
         {
            if (_engineState.currentTrackIndex > 0)
            {
               _backend.GotoPrevFile();
               _UpdateButtonState();
               _SetPendingUpdate(); // Track is changing...
            }
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
               "tcp://" + serverName + ":" + serverPort + "/Engine";

            _Status( serverUrl + " - Trying...", 10 );

            // Retrieve a reference to the remote object
            IEngine engine = (IEngine) Activator.GetObject( typeof(IEngine), 
                                                            serverUrl );

            _Status( serverUrl + " - Connected", 10 );
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

      void _OnIsSuckActiveToggled( object sender, EventArgs args )
      {
         _Trace( "_OnIsSuckActiveToggled" );
         try
         {
            if (_isSuckActiveBtn.Active)
               _backend.ActivateCriterion( 0 );
            else
               _backend.DeactivateCriterion( 0 );

            _SetPendingUpdate();
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _OnIsAppropriateActive1Toggled( object sender, EventArgs args )
      {
         /// \todo Have the appropriate toggles share 1 function.
         ///
         _Trace( "_OnIsAppropriateActive1Toggled" );
         try
         {
            if (_isAppropriateActive1Btn.Active)
               _backend.ActivateCriterion( _appropriateKey );
            else
               _backend.DeactivateCriterion( _appropriateKey );

            _SetPendingUpdate();
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }


      void _SetPendingUpdate()
      {
         _pendingUpdate = true;
         _titleDisplay.Buffer.Text = "";
         _artistDisplay.Buffer.Text = "";
         _albumDisplay.Buffer.Text = "";
         _pathDisplay.Buffer.Text = "";

         _UpdateNow();
      }

      ///
      /// Here we decide which controls are greyed out, etc, based
      /// on the current state.
      ///
      void _UpdateButtonState()
      {
         _Trace( "[_UpdateButtonState]" );

         bool isConnected = (null != _engineState);

         _isSuckActiveBtn.Sensitive = isConnected;
         _isAppropriateActive1Btn.Sensitive = isConnected;
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

         if (!isConnected || 
             !_engineState.isPlaying || 
             _pendingUpdate)
         {
            _suckBtn.Sensitive = false;
            _notAppropriateBtn.Sensitive = false;
            _suckSlider.Sensitive = false;
            _appropriateSlider.Sensitive = false;
         }
         else
         {
            _suckSlider.Sensitive = _isSuckActive;
            _suckBtn.Sensitive = _isSuckActive;

            _appropriateSlider.Sensitive = _isAppropriateActive;
            _notAppropriateBtn.Sensitive = _isAppropriateActive;

            if (_engineState.currentTrackIndex <= 0)
               _prevBtn.Sensitive = false;
            else
               _prevBtn.Sensitive = true;
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

      ListStore _historyListStore; // what we've played
      ListStore _futureListStore; // what we're gonna play

      // State for the configurable attributes. I guess.

      bool   _isAppropriateActive = false;
      uint   _appropriateKey = 1;
      double _appropriateValue;

      bool   _isSuckActive = false;

      double _suckValue;

      // Misc

      bool _pendingUpdate = true;

      //
      // -- Widgets that are attached to Glade
      //
      [Glade.Widget]
      TreeView _historyListView;

      [Glade.Widget]
      TreeView _futureListView;

      [Glade.Widget]
      TextView _titleDisplay;

      [Glade.Widget]
      TextView _artistDisplay;

      [Glade.Widget]
      TextView _pathDisplay;

      [Glade.Widget]
      TextView _albumDisplay;

      [Glade.Widget]
      Scale  _suckSlider;

      [Glade.Widget]
      Scale  _appropriateSlider;

      [Glade.Widget]
      CheckButton _isSuckActiveBtn;

      [Glade.Widget]
      CheckButton _isAppropriateActive1Btn;

//       [Glade.Widget]
//       Button _isAppropriateActiveBtn;

      [Glade.Widget]
      Button _suckBtn;

      [Glade.Widget]
      Button _notAppropriateBtn;

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
