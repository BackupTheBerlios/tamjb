/// \file
/// $Id$
///

// Copyright (C) 2006-2007 Tom Surace.
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

namespace byteheaven.tamjb.webgui
{
   using System;
   using System.Collections;
   using System.Data;
   using System.Web;
   using System.Web.Security;
   using System.Web.UI.WebControls;

   using Anthem;
   using byteheaven.tamjb.Interfaces;

   public class index : byteheaven.tamjb.webgui.WebPageBase
   {
      protected Anthem.Timer refreshTimer;

      protected Anthem.LinkButton userNameBtn;
      protected Anthem.LinkButton moodBtn;

      protected Anthem.Label nowSuckLevel;
      protected Anthem.Label nowMoodLevel;

      protected Anthem.Label nowTitle;
      protected Anthem.Label nowArtist;
      protected Anthem.Label nowAlbum;
      protected Anthem.Label nowFileName;

      protected Anthem.Button megaSuckBtn;

      protected Anthem.Button refreshButton;

      protected Anthem.CheckBox showPast;
      protected Anthem.CheckBox showFuture;
      protected Anthem.Panel    historyBox;
      protected Anthem.Repeater history;

      protected uint _userId;

      // temporarily (?) hard-coded history size
      //
      const int MAX_HISTORY = 5;

      override protected void OnLoad( EventArgs loadArgs )
      {
         base.OnLoad( loadArgs );

         Manager.Register( this );

         // Hack to allow unauthenticated users to see what is playing 
         System.Security.Principal.IIdentity identity = 
            HttpContext.Current.User.Identity;

         if (! identity.IsAuthenticated)
         {
            throw new ApplicationException( 
               "Internal error, anonymous access not supported" );
         }
         _userId = Convert.ToUInt32( identity.Name );
         Credentials credentials = backend.RenewLogon( _userId );
         if (null == credentials)
         {
            FormsAuthentication.RedirectToLoginPage();
         }

         try
         {
            // Make sure the mood button does a postback instead of
            // trying to use AJAX, cause we want to server.transfer:
            moodBtn.EnableCallBack = false;

            if (!IsPostBack)
            {
               historyBox.Visible = false;
               ViewState["pastVisible"] = false;
               ViewState["futureVisible"] = false;
               showPast.Checked = false;
               showFuture.Checked = false;
            }
            
            if (Anthem.Manager.IsCallBack)
            {
               // Workaround for the "OnCheckedChanged handler never called"
               // bug:
               if (null == ViewState["pastVisible"] 
                   || 
                   ((bool)ViewState["pastVisible"] != showPast.Checked))
               {
                  _PastToggle();
                  ViewState["pastVisible"] = showPast.Checked;
               }

               if (null == ViewState["futureVisible"] 
                   || 
                   ((bool)ViewState["futureVisible"] != showFuture.Checked))
               {
                  _FutureToggle();
                  ViewState["futureVisible"] = showFuture.Checked;
               }
            }
            else
            {
               userNameBtn.Text = credentials.name;
            }
         }
         catch (Exception)
         {
            throw;              // let it fly
         }
      }

      override protected void OnPreRender( EventArgs loadArgs )
      {
         base.OnPreRender( loadArgs );

         try
         {
            _Refresh();
         }
         catch (Exception)
         {
            throw;              // not handled yet
         }
      }


      ///
      /// Update the history panel visibility
      /// 
      void _PastToggle()
      {
         historyBox.Visible = showPast.Checked || showFuture.Checked;
         historyBox.UpdateAfterCallBack = true;
         
         // Force a refresh of the history box
         changeCount = -1;
      }

      ///
      /// Update the future panel visibility
      /// 
      void _FutureToggle()
      {
         historyBox.Visible = showPast.Checked || showFuture.Checked;
         historyBox.UpdateAfterCallBack = true;
         
         // Force a refresh of the history box
         changeCount = -1;
      }

      ///
      /// Refresh all now-playing related display:
      ///
      protected void _OnRefresh( object sender, EventArgs ea )
      {
         _Refresh();
      }

      //
      // Note signature must be right for Anthem_InvokePageMethod to work,
      // which is to say it must be public and return int!
      //
      [Anthem.Method]
      public int _Refresh()
      {
         try
         {
            long newChangeCount = backend.changeCount;
            if (newChangeCount != changeCount)
            {
               Console.WriteLine( "  Engine state changed {0} -> {1}",
                                  changeCount, newChangeCount );

               Mood mood = new Mood();
               backend.GetCurrentMood( _userId, ref mood );

               // Save what we THINK is the current state for later.
               this.mood = mood;
            
               EngineState state = backend.GetState();
               _UpdateNowPlayingInfo( state );
               _BuildGrid( state );

               changeCount = state.changeCount; // save for later

               refreshButton.Text = changeCount.ToString();
               refreshButton.UpdateAfterCallBack = true;
            }

            return 0;
         }
         catch ( System.Net.WebException snw )
         {
            // OK, so this error message will be unpleasant.
            string msg = "Couldn't connect to the server";
            
            // For now just rethrow.
            throw new ApplicationException( msg, snw );
         }
      }

      void _BuildGrid( EngineState state )
      {
         // Can't trust the value of historyBox.Visible. Viewstate problem, maybe?
         if (!(showPast.Checked || showFuture.Checked))
            return;

         DataTable table = new DataTable();
         table.Columns.Add("key", typeof(uint));
         table.Columns.Add("title", typeof(string));
         table.Columns.Add("artist", typeof(string));
         table.Columns.Add("album", typeof(string));
         table.Columns.Add("suck", typeof(int));
         table.Columns.Add("mood", typeof(int));
         table.Columns.Add("status", typeof(string));
         table.Columns.Add("when", typeof(string));
         table.Columns.Add("probability", typeof(string));

         int index = 0;
	 int lastIndex = 0;

         if (! showPast.Checked) // Don't show the past at all?
         {
            index = state.currentTrackIndex;
         }
         else
         {
	    // Only show a little history:
            if (state.currentTrackIndex > MAX_HISTORY)
            {
               index = state.currentTrackIndex - MAX_HISTORY;
            }
	    else
            {
               index = 0;
            }
         }

	 if (index < 0) // In case currentTrackIndex is negative?
            index = 0;

	 lastIndex = state.playQueue.Length;
	 
         while (index < lastIndex)
         {
            ITrackInfo info  = state.playQueue[index];

            int suck;
            int mood;
            _GetSuckAsInt( info.key, out suck, out mood );

            DataRow row = table.NewRow();
            row["key"] = info.key;
            row["title"] = info.title;
            row["artist"] = info.artist;
            row["album"] = info.album;
            row["suck"] = suck;
            row["mood"] = mood;
            
            if (TrackStatus.MISSING == info.status)
            {
               row["status"] = "MISSING";
               row["title"] += " (MISSING)";
            }
            else
            {
               row["status"] = info.evaluation.ToString();
            }

            if (index < state.currentTrackIndex)
            {
               row["when"] = "past";
            }
            else if (index == state.currentTrackIndex)
            {
               row["when"] = "present";
            }
            else
            {
               if (! showFuture.Checked) // Not showing future tracks?
                  break;                 // ** Quick Exit **

               row["when"] = "future";
            }

            // What are the chances this will play? Divide into 5 ranges.
            // Something below 10% or above 90% never plays, so that's 
            // intervals of 16%:

            int prob = (100 - suck) * mood;
            if (prob < (26 * 26))     // 10 * 10 = 100 - never play
               row["probability"] = "probLow";
            else if (prob < (42 * 42))
               row["probability"] = "probMedLow";
            else if (prob < (58 * 58))
               row["probability"] = "probMed";
            else if (prob < (74 * 74))
               row["probability"] = "probMedHigh";
            else
               row["probability"] = "probHigh";

            table.Rows.Add(row);
	 
	    ++index;
         }

         history.DataSource = table;
         history.DataBind();  
         history.UpdateAfterCallBack = true;
      }

      ///
      /// get/set the change count so we know if a refresh is needed.
      ///
      long changeCount
      {
         get
         {
            if (null == ViewState["changeCount"])
            {
               return -1;       // Not known.
            }
            
            return (long)ViewState["changeCount"];
         }

         set
         {
            ViewState["changeCount"] = value;
         }
      }

      ///
      /// Retrieves what we think is the now playing track from viewstate:
      ///
      uint currentTrack
      {
         get
         {
            if (null == ViewState["nowTrackKey"])
            {
               throw new ApplicationException( 
                  "now playing track key missing from view state" );
            }
            
            return (uint) ViewState["nowTrackKey"];
         }

         set
         {
            ViewState["nowTrackKey"] = value;
         }
      }

      ///
      /// get/save the current mood in the viewstate
      ///
      Mood mood
      {
         get
         {
            if (null == ViewState["moodIdKey"])
            {
               throw new ApplicationException( 
                  "mood key not in viewstate" );
            }

            uint moodId = (uint)ViewState["moodIdKey"];
            return new Mood( moodBtn.Text, moodId );
         }

         set
         {
            moodBtn.Text = value.name;
            moodBtn.UpdateAfterCallBack = true;
            ViewState["moodIdKey"] = value.id;
         }
      }



      void _UpdateNowPlayingInfo( EngineState engineState )
      {
         ITrackInfo current = engineState.currentTrack;

         // Save for later.
         this.currentTrack = current.key;

         nowTitle.Text = current.title;
         nowTitle.UpdateAfterCallBack = true;

         nowArtist.Text = current.artist;
         nowArtist.UpdateAfterCallBack = true;
         
         nowAlbum.Text = current.album;
         nowAlbum.UpdateAfterCallBack = true;
         
         nowFileName.Text = current.filePath;
         nowFileName.UpdateAfterCallBack = true;

         int suck;
         int mood;
         _GetSuckAsInt( current.key, out suck, out mood );
         nowSuckLevel.Text = suck.ToString();
         nowSuckLevel.UpdateAfterCallBack = true;
         nowMoodLevel.Text = mood.ToString();
         nowMoodLevel.UpdateAfterCallBack = true;
      }

      void _GetSuckAsInt( uint key, out int suck, out int mood )
      {
         double suckPercent;
         double moodPercent;
         backend.GetAttributes( _userId,
                                this.mood.id,
                                key,
                                out suckPercent,
                                out moodPercent );

         suckPercent /= 100;
         suck = (int)suckPercent;

         moodPercent /= 100;
         mood = (int)moodPercent;
      }

      protected void _OnUserClick( object sender, EventArgs ea )
      {
         try
         {
            // TODO: use css layers to make a cool user popup here
            // or just go to a freaking form. Either one.
         }
         catch (Exception)
         {
         }
      }

      protected void _OnMoodClick( object sender, EventArgs ea )
      {
         Server.Transfer( "moodselect.aspx" );
      }

      public void _OnSuck( object sender, EventArgs ea )
      {
         try
         {
            EngineState engineState = backend.GetState();

            // The engine could be rewound past 0 or whatever by some
            // other user.
            if (engineState.currentTrackIndex < 0)
               return;

            // Flag the previously playing track as suck
            backend.IncreaseSuckZenoStyle( _userId, 
                                           this.currentTrack );

            // Reevaluate this track based on its new suck level, if
            // it is currently playing.
            //
            // TODO: the engine should really be controlling when this
            // does or does not happen.
	    //
	    // BUG: should pass this.currentTrack as a parameter to the
	    // Reevaulate call, just in case it changed in the interim.
            //
            if (engineState.currentTrack.key == this.currentTrack)
               backend.ReevaluateCurrentTrack();

         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

      public void _OnMegaSuck( object sender, EventArgs ea )
      {
         try
         {
            EngineState engineState = backend.GetState();

            // The engine could be rewound past 0 or whatever by some
            // other user.
            if (engineState.currentTrackIndex < 0)
               return;

            // todo: there should be a way to do this with one call:
            for (int i = 0; i < 3; i++)
            {
               backend.IncreaseSuckZenoStyle( _userId, 
                                              this.currentTrack );
            }

            // ANd, unconditionally go to the next track
            backend.GotoNextFile( _userId, this.currentTrack );

         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

      protected void _OnRule( object sender, EventArgs ea )
      {
         try
         {
            EngineState engineState = backend.GetState();
            if (engineState.currentTrackIndex < 0)
               return;

            // Flag the previously playing track as less sucky
            backend.DecreaseSuckZenoStyle( _userId, 
                                           this.currentTrack );

         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

      protected void _OnYes( object sender, EventArgs ea )
      {
         try
         {
            EngineState engineState = backend.GetState();
            if (engineState.currentTrackIndex < 0)
               return;

            // Flag the previously playing track as less sucky
            backend.IncreaseAppropriateZenoStyle( _userId, 
                                                  this.mood,
                                                  this.currentTrack );

         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

      protected void _OnNo( object sender, EventArgs ea )
      {
         try
         {
            EngineState engineState = backend.GetState();
            if (engineState.currentTrackIndex < 0)
               return;

            // Flag the previously playing track as less sucky
            backend.DecreaseAppropriateZenoStyle( _userId, 
                                                  this.mood,
                                                  this.currentTrack );

            if (engineState.currentTrack.key == this.currentTrack)
               backend.ReevaluateCurrentTrack();

         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

      protected void _OnPrev( object sender, EventArgs ea )
      {
         try
         {
            EngineState engineState = backend.GetState();
            if (engineState.currentTrackIndex < 0)
               return;

            // Go to the track before "this" one
            backend.GotoPrevFile( _userId, this.currentTrack );

         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

      protected void _OnNext( object sender, EventArgs ea )
      {
         try
         {
            backend.GotoNextFile( _userId, this.currentTrack );
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

      protected void _OnStop( object sender, EventArgs ea )
      {
         try
         {
            backend.StopPlaying();
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

      protected void _OnPlay( object sender, EventArgs ea )
      {
         try
         {
            backend.StartPlaying();
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }


      ///
      /// Handler for the javascript callbacks from the history grid.
      /// (Makes use of viewstate unnecessary)
      ///
      [Anthem.Method]
      public void _OnHistoryCommand( string command,
                                     string keyString )
                                     
      {
         try
         {
            uint key = Convert.ToUInt32( keyString ) ;
            Console.WriteLine( "HistoryCommand {0}-{1}", command, keyString );

            switch (command)
            {
            case "suckMore":
               backend.IncreaseSuckZenoStyle( _userId, key );
               break;

            case "suckLess":
               backend.DecreaseSuckZenoStyle( _userId, key );
               break;

            case "moodYes":
               backend.IncreaseAppropriateZenoStyle( _userId, 
                                                     this.mood,
                                                     key );
               break;

            case "moodNo":
               backend.DecreaseAppropriateZenoStyle( _userId, 
                                                     this.mood,
                                                     key );
               break;

            default:
               throw new ApplicationException( "Unexpected repeater command" );
            }
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }

   }
}
