/// \file
/// A minimal music player.
///
/// $Id$
///

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

   // Need to include things that would  normally be hidden to get
   // the data structs they use. Should these be moved into the global
   // namespace to make them available at the "tam" namespace scope?
   
   ///
   /// SimplePlayer -- Gtk# frontend to the SimpleMp3Jukebox
   ///
   /// \todo add configuration and initialisation dialogs.
   ///
   /// \todo Add code to monitor the status of the Mp3Player threads
   ///   and restart or pop up dialogs or whatever on failure. Etc.
   ///
   /// \todo make sure try/catch blocks are around all Gtk event handling
   ///   callbacks, and make a nice diagnostic dialog to go with.
   ///
   public class MainWnd : Window
   {
      // Some constants
      readonly int TITLE_MAX_WIDTH = 50;
      readonly int ARTIST_MAX_WIDTH = 20;
      // What's comin' at cha, including the current track.
      readonly int QUEUE_MIN_SIZE = 6; 
      readonly int HISTORY_VISIBLE_COUNT = 5;

      // Temporary hardcoded table index for the suck metric
      readonly uint DOESNTSUCK = 0;

      // readonly string SETTINGS_FILE = "/home/tekhedd/.tam.GtkPlayerrc";

      static void Main( string [] args )
      {
         try
         {
            // Create a channel for communicating w/ the remote object
            // Should not have to explicitly state BinaryClient, should I?

            // HttpChannel channel = new HttpChannel();
            ListDictionary properties = new ListDictionary();
            HttpChannel channel = 
               new HttpChannel(properties,
                               new BinaryClientFormatterSinkProvider(),
                               new BinaryServerFormatterSinkProvider());

            ChannelServices.RegisterChannel( channel );

            // TcpChannel tcpChannel = new TcpChannel();
            // ChannelServices.RegisterChannel( tcpChannel );
            
            Application.Init ();

            MainWnd player = new MainWnd();
         
            player.Show();

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
         Console.WriteLine( "Main> bye" );

         // If any stray threads are around, deal with it here.
      }

      ///
      /// Constructs the main window layout and all component bits.
      ///
      public MainWnd()
         : base( "tam.MainWnd" )
      {
         // Deal with unexpected window closure
         this.DeleteEvent += new DeleteEventHandler( _DeleteHandler );

         this.Name = "tam.gtkPlayer";

         VBox topBox = new VBox( false, 0 );

         // -- Row 0, 1

         // or ListStore?
         // Columns: name, artist, track info
         _historyListStore = new ListStore( typeof(string),
                                            typeof(string),
                                            typeof(ITrackInfo) ); 

         TreeView historyListView = new TreeView( _historyListStore );
         historyListView.HeadersVisible = false;
         historyListView.HeadersClickable = false;
         historyListView.EnableSearch = false;
         historyListView.AppendColumn( "Artist", 
                                       new CellRendererText(),
                                       "text",
                                       0 );

         historyListView.AppendColumn( "Track Name", 
                                       new CellRendererText(),
                                       "text",
                                       1 );

         for (int i = 0; i < HISTORY_VISIBLE_COUNT; i++)
            _historyListStore.AppendValues( "", "", null );

         topBox.PackStart( historyListView, false, false, 0 );

         //
         // The now playing box shows what is now playing. And stuff.
         //


         Frame labelFrame = new Frame();
         labelFrame.ShadowType = ShadowType.In;

         // Table goes inside this shady frame

         Table tbl = new Table( 3,  3, false /* all same size */ );
         uint top = 0;          // top and botton of current row;
         uint bottom = 1;

         HBox labelStuff = new HBox( false, 0 );

         _titleDisplay = new Entry( "(title)" );
         _titleDisplay.Name = "titleBox";
         _titleDisplay.Editable = false;
         _titleDisplay.HasFrame = false;
         _titleDisplay.WidthChars = TITLE_MAX_WIDTH + 2;

         labelStuff.PackEnd( _titleDisplay, true, false, 0 );

         _artistDisplay = new Entry( "(artist)" );
         _artistDisplay.Name = "artistBox";
         _artistDisplay.Editable = false;
         _artistDisplay.HasFrame = false;
         _artistDisplay.WidthChars = ARTIST_MAX_WIDTH + 2;

         labelStuff.PackEnd( _artistDisplay, true, false, 0 );

         EventBox backgroundWidget = new EventBox();
         backgroundWidget.Name = "titleBg";
         backgroundWidget.Add( labelStuff );

         tbl.Attach( backgroundWidget, 0, 3, top, bottom,
                     AttachOptions.Fill, AttachOptions.Shrink, 0, 0 );


         ++top; ++bottom; // -- Suck control

         _suckSlider = new HScale( 0.0, 100.0, 1.0 );
         _suckSlider.Digits = 1;
         _suckSlider.Value = 21.3;
         _suckSlider.UpdatePolicy = UpdateType.Discontinuous;
         _suckSlider.ValueChanged += new EventHandler( _OnSuckSliderChanged );

         tbl.Attach( _suckSlider, 0, 1, top, bottom );
                     // AttachOptions.Fill, AttachOptions.Fill, 0, 0 );

         _suckBtn = new Button ("_Sucks");
         _suckBtn.Clicked += new EventHandler( _SuckBtnClick );
         tbl.Attach( _suckBtn, 1, 2, top, bottom );


         ++top; ++bottom;

         //
         // Attribute display
         //
         _appropriateSlider = new HScale( 0.0, 100.0, 1.0 );
         _appropriateSlider.Digits = 1;
         _appropriateSlider.Value = 21.3;
         _appropriateSlider.UpdatePolicy = UpdateType.Discontinuous;
         _appropriateSlider.ValueChanged += 
            new EventHandler( _OnAppropriateSliderChanged );

         tbl.Attach( _appropriateSlider, 0, 1, top, bottom );

         // tbl.Attach( _wrongBtn, 1, 2, top, bottom );
         // 
         // tbl.Attach( _rightBtn, 2, 3, top, bottom,
         //      AttachOptions.Fill, AttachOptions.Fill, 0, 0 );


         labelFrame.Name = "titleBackground";
         labelFrame.Add( tbl );

         topBox.PackStart( labelFrame, true, true, 2 );

         ++top; ++bottom;
         // 
         // The list of what is coming up!
         //

         _futureListStore = new ListStore( typeof(string),
                                           typeof(string),
                                           typeof(bool) ); 

         TreeView futureListView = new TreeView( _futureListStore );
         futureListView.HeadersVisible = false;
         futureListView.HeadersClickable = false;
         futureListView.EnableSearch = false;
         TreeViewColumn column;
         column = new TreeViewColumn( "Artist", 
                                      new CellRendererText(),
                                      "text",
                                      0 );
         column.Sizing = TreeViewColumnSizing.GrowOnly;
         futureListView.AppendColumn( column );

         column = new TreeViewColumn( "Track Name", 
                                      new CellRendererText(),
                                      "text",
                                      1 );
         column.Sizing = TreeViewColumnSizing.GrowOnly;
         futureListView.AppendColumn( column );

         // Should be a checkbox or something. Hmmm.
         column = new TreeViewColumn( "Suck", 
                                      new CellRendererToggle(),
                                      "active",
                                      2 );
         column.Sizing = TreeViewColumnSizing.Fixed; // Autosize, GrowOnly
         // column.Toggled = new ToggledHandler( _OnSuckToggled );
         futureListView.AppendColumn( column );

         // Create rows to store data in (so the damn thing won't be
         // always growing and shrinking):
         for (int i = 0; i < (QUEUE_MIN_SIZE - 1); i++)
            _futureListStore.AppendValues( "", "", false );

         topBox.PackStart( futureListView, false, false, 0 );



         HBox hbox = new HBox( true, 2 );

         _prevBtn = new Button( "_Prev" );
         _prevBtn.Clicked += new EventHandler( _PrevBtnClick );
         hbox.Add( _prevBtn );

         Button skipBtn = new Button ("_Next");
         skipBtn.Clicked += new EventHandler( _SkipBtnClick );
         hbox.Add( skipBtn );

         topBox.PackStart( hbox, false, false, 0 );


         ++top; ++bottom; // --- The config box 

         hbox = new HBox( false, 2 );

         // This will probably be some more complicated control or
         // something. Maybe. Or I could simply have an add dropdown option.
         _playlistSel = new Combo();
         _playlistSel.Entry.IsEditable = false;
         _listOptions = new string[] { "(all files)",
                                       "Tom",
                                       "Renee",
                                       "Games" };

         // Note: interface may change
         _playlistSel.SetPopdownStrings( _listOptions );
         _playlistSel.Entry.Changed += 
            new EventHandler( _OnPlaylistSelection );

         hbox.Add( _playlistSel );

         Button configBtn = new Button ("_Config");
         configBtn.Clicked += new EventHandler( _ConfigBtnClick );
         hbox.Add( configBtn );

         topBox.PackStart( hbox, false, false, 0 );


         // --

         this.Add( topBox );
         this.ShowAll();


         //
         // Create database connection -- 
         //

         try
         {
            _settings = PlayerSettings.Fetch();
         }
         catch (Exception e)
         {
            _settings = new PlayerSettings();
            _settings.serverName = "localhost";
            _settings.serverPort = 5432;
         }

         try
         {
            /// \todo make the database connection configurable, and move this
            ///   code into another class. Or at least into a function.
            ///
            _backend = _ConnectToEngine( _settings.serverName,
                                         _settings.serverPort );

            // Default: active suck criterion
            _backend.ActivateCriterion( 0 );
         }
         catch (Exception e)
         {
            // OK, so the backend isn't reachable
            _backend = null;
         }

         /// \todo Retrieve settings from storage here?
         ///

         _configDlg = new ConfigDlg( this, _backend, _settings );

         _UpdateButtonState();

         // Status watcher callback.
         Gtk.Timeout.Add( 2000, new Gtk.Function( _PollingCallback ) );
      }

      ~MainWnd()
      {
         // Save settings?
         _settings.Store();
         Console.WriteLine( "MainWnd> bye" );
      }

      // Gtk.ToggledHandler not implemented...at this time.
//       void _OnSuckToggled( object o, ToggledArgs args )
//       {
//       }

      void _OnSuckSliderChanged( object sender, EventArgs e )
      {
         try
         {
            Console.WriteLine( "SuckChanged" );
            
            _OnSliderChanged( _suckSlider,
                              ref _suckValue,
                              DOESNTSUCK,
                              true ); // inverted
         }
         catch (Exception ex)
         {
            Console.WriteLine( ex.ToString() );
         }
      }

      void _OnAppropriateSliderChanged( object sender, EventArgs e )
      {
         try
         {
            Console.WriteLine( "AppropriateChanged" );
            _OnSliderChanged( _appropriateSlider,
                              ref _appropriateValue,
                              (uint)_runningPlaylistKey,
                              false ); // not inverted
         }
         catch (Exception ex)
         {
            Console.WriteLine( ex.ToString() );
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
                  
         _SetPendingUpdate();

         // Flag the previously playing track as suck
         _backend.SetAttribute( attributeKey, trackKey, (uint)newValue );
      }

      ///
      /// This is called periodically as a Timeout to check the status
      /// of the player and so on. 
      ///
      bool _PollingCallback()
      {
         try
         {
            // State changed?
            if (_backend.CheckState(ref _engineState) || _pendingUpdate )
               _UpdateNowPlayingInfo();
         }
         catch (Exception e)
         {
            ///
            /// \todo Should we go to an "establishing connection" 
            ///   interface here (and allow reconfiguration)?
            ///
            Console.WriteLine( "Could not update displayed track info: " 
                               + e.ToString() );
         }

         return true; // keep calling
      }
      
      void _OnPlaylistSelection( object o, EventArgs args )
      {
         try
         {
            string selection = _playlistSel.Entry.Text;

            // Suck always active for now
            _backend.ActivateCriterion( 0 );

            /// \todo Make playlist gui allow for arbitrary number
            ///   of simultaneous criteria.

            _backend.DeactivateCriterion( 1 );
            _backend.DeactivateCriterion( 2 );
            _backend.DeactivateCriterion( 3 );
            if (selection == "(all files)")
            {
               _backend.DeactivateCriterion( 1 );
               _runningPlaylistKey = 0;
            }
            else if (selection == "Tom")
            {
               _backend.ActivateCriterion( 1 );
               _runningPlaylistKey = 1;
            }
            else if (selection == "Renee")
            {
               _backend.ActivateCriterion( 2 );
               _runningPlaylistKey = 2;
            }
            else if (selection == "Games")
            {
               _backend.ActivateCriterion( 3 );
               _runningPlaylistKey = 3;
            }

            _UpdateButtonState();
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
         }
      }

      ///
      /// Update the "now playing" display boxes and so on
      ///
      /// \throw Exception and friends. No attempt is made to catch them.
      ///
      void _UpdateNowPlayingInfo()
      {
         Console.WriteLine( "_UpdateNowPlayingInfo" );

         ITrackInfo trackInfo = null;
         if (null != _engineState)
            trackInfo = _engineState.currentTrack;

         if (null == trackInfo) // not playing after all
         {
            _titleDisplay.Text = "(stopped)";
            _artistDisplay.Text = "";
         }
         else
         {
            _pendingUpdate = false;

            uint doesntSuckLevel = _backend.GetAttribute( DOESNTSUCK, 
                                                          trackInfo.key );

            // Invert the doesntSuck value to create a suck metric
            double suckPercent = (10000 - doesntSuckLevel) / 100.0;

            _titleDisplay.Text = _FixTitle( trackInfo );
            _artistDisplay.Text = trackInfo.artist;
            _suckSlider.Value = suckPercent;
            _suckValue = suckPercent;

            if ( _runningPlaylistKey != 0 )
            {
               double appropriateLevel = 
                  (double)_backend.GetAttribute( (uint)_runningPlaylistKey,
                                                 trackInfo.key );

               appropriateLevel /= 100.0;
               _appropriateSlider.Value = appropriateLevel;
               _appropriateValue = appropriateLevel;
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
                  + title.Substring( title.Length - TITLE_MAX_WIDTH - 4 );
            }
            
            return title;
         }
      }

      /// Here we decide which controls are greyed out, etc, based
      /// on the current state.
      void _UpdateButtonState()
      {
         // Console.WriteLine( "_UpdateButtonState" );

         // Implement me! :)
         // check _nowPlaying, _currentTrackKey, and _pendingUpdate etc
         if (null == _engineState || 
             !_engineState.isPlaying || 
             _pendingUpdate)
         {
            _playlistSel.Sensitive = false;
            _suckBtn.Sensitive = false;
            _suckSlider.Sensitive = false;
            _appropriateSlider.Sensitive = false;
            _prevBtn.Sensitive = false;
         }
         else
         {
            _playlistSel.Sensitive = true;
            _suckBtn.Sensitive = true;
            _suckSlider.Sensitive = true;

            if (_runningPlaylistKey > 0)
            {
               _appropriateSlider.Sensitive = true;
            }
            else
            {
               _appropriateSlider.Sensitive = false;
            }

            if (_engineState.currentTrackIndex <= 0)
               _prevBtn.Sensitive = false;
            else
               _prevBtn.Sensitive = true;
         }
      }

      void _SetPendingUpdate()
      {
         // TODO: disable lots of buttons here?
         if (!_pendingUpdate)
         {
            _pendingUpdate = true;
            _titleDisplay.Text = "---";
            _artistDisplay.Text = "";
         }
      }

      ///
      /// Called when the user things this song is wrong for the playlist.
      ///
      void _WrongBtnClick( object sender, EventArgs args )
      {
         try
         {
            // Bail out if this is 0-- it means no playlist is loaded
            if (_runningPlaylistKey == 0)
               return;

            ITrackInfo info = _engineState.currentTrack;
               
            // Send the player to the next file first.
            _SetPendingUpdate();
            _backend.GotoNextFile();

            // Now that it's no longer playing, update the track.
            _backend.DecreaseAttributeZenoStyle( _runningPlaylistKey, 
                                                 info.key );
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
         }
      }

      ///
      /// Called when the user things this song is wrong for the playlist.
      ///
      void _RightBtnClick( object sender, EventArgs args )
      {
         try
         {
            // Bail out if this is 0-- it means no playlist is loaded
            if (_runningPlaylistKey == 0)
               return;

            Console.WriteLine( "MainWnd: Right! (" + 
                               _runningPlaylistKey +
                               ")" );

            ITrackInfo info = _engineState.currentTrack;
            _backend.IncreaseAttributeZenoStyle( _runningPlaylistKey, 
                                                 info.key );
         
            _UpdateNowPlayingInfo(); // update the display, etc.
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
         }
      }

      void _SuckBtnClick( object sender, EventArgs args )
      {
         try
         {
            Console.WriteLine( "MainWnd: Sucks" );

            if (_engineState.currentTrackIndex >= 0)
            {
               // Whether the suck attribute table is active or not, 
               // send feedback to the suck table.

               uint suckTrackKey = _engineState.currentTrack.key;

               // Stop playing this track NOW
               _SetPendingUpdate();
               _backend.GotoNextFile(); // this takes a while

               // Flag the previously playing track as suck
               _backend.DecreaseAttributeZenoStyle( DOESNTSUCK, suckTrackKey );
            }
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
         }
      }

      void _SkipBtnClick( object sender, EventArgs args )
      {
         try
         {
            _SetPendingUpdate();
            _backend.GotoNextFile();
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
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
               _SetPendingUpdate(); // Track is changing...
               _backend.GotoPrevFile();
               _UpdateButtonState();
            }
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
         }
      }

      ///
      /// Run the configuration dialog.
      ///
      void _ConfigBtnClick( object sender, EventArgs args )
      {
         try
         {
            Console.WriteLine( "MainWnd: Config" );

            if ((int)ResponseType.Ok == _configDlg.Run())
            {
               Console.WriteLine( "OK" );

               // (re)connect to the player
               _backend = null;
               _backend = _ConnectToEngine( _settings.serverName,
                                            _settings.serverPort );
            }
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
         }
      }

      ///
      /// Called when the window is deleted via the window manager
      ///
      void _DeleteHandler( object sender, DeleteEventArgs delArgs )
      {
         Console.WriteLine( "MainWnd: Closed By Request" );
         Application.Quit();
      }

      IEngine _ConnectToEngine( string serverName,
                                int serverPort )
      {
         try
         {
            string serverUrl = 
               "http://" + serverName + ":" + serverPort + "/Engine";

            Console.WriteLine( serverUrl );

            // Retrieve a reference to the remote object
            IEngine engine = (IEngine) Activator.GetObject( typeof(IEngine), 
                                                            serverUrl );

            return engine;
         }
         catch ( System.Net.WebException snw )
         {
            // Probably couldn't connect. Use friendly message. Where is
            // that advanced error msg dialog I want?
            string msg = "Couldn't connect to the server '" 
               + serverName + ":" + serverPort + "'";

            // For now just rethrow. (Ick!)
            throw new ApplicationException( msg, snw );
         }
      } 



      //
      // Controls
      //

      Entry  _titleDisplay;
      Entry  _artistDisplay;
      Scale  _suckSlider;
      double _suckValue;
      Scale  _appropriateSlider;
      double _appropriateValue;
      Combo  _playlistSel;
      Button _suckBtn;
      Button _prevBtn;

      ListStore _historyListStore; // what we've played
      ListStore _futureListStore; // what we're gonna play

      //
      // Current state and other misc vars
      //

      string [] _listOptions;

      // Engine state as of our last update:
      IEngineState _engineState = null;
      //       bool   _nowPlaying = false;
      //       uint   _currentTrackKey = 0;
      
      ///
      /// This is set true if we are expecting the engine state
      /// to change (ie, at times when a progress dialog would
      /// be more appropriate).
      ///
      bool   _pendingUpdate = false;

      PlayerSettings _settings;
      ConfigDlg      _configDlg;

      ///
      /// While the SUCK playlist is always loaded, this playlist is
      /// superimposed on it.
      /// 
      uint _runningPlaylistKey = 0;  // default to 0 (suck) meaning disabled

      tam.IEngine _backend;

   }
}
