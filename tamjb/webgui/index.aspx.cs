/// \file
/// $Id$
///

// Copyright (C) 2006 Tom Surace.
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
   using System.Web;
   using System.Web.UI.WebControls;

   using Anthem;
   using byteheaven.tamjb.Interfaces;

   public class index : byteheaven.tamjb.webgui.WebPageBase
   {
      protected Anthem.LinkButton userNameBtn;
      protected Anthem.LinkButton moodBtn;

      protected Anthem.Label nowSuckLevel;
      protected Anthem.Label nowMoodLevel;

      protected Anthem.Label nowTitle;
      protected Anthem.Label nowArtist;
      protected Anthem.Label nowAlbum;
      protected Anthem.Label nowFileName;

      protected Anthem.Button refreshButton;

      override protected void OnLoad( EventArgs loadArgs )
      {
         base.OnLoad( loadArgs );

         try
         {
            Manager.Register( this );

            if (! IsPostBack)
            {
               _InstallRefreshScript();

               _Refresh();
            }
         }
         catch (Exception)
         {
            // do nothing
         }
      }

      ///
      /// \todo make the refresh interval configurable
      ///
      void _InstallRefreshScript()
      {
         string timeout = "10000"; // milliseconds, I hope!

         // A script that sets another timeout, then calls the "refresh"
         // button's callback.
         string refreshFunction = 
            "<script>\n"
            + "<!--\n"
            + "function doRefresh() {\n"
            + " setTimeout(\"doRefresh()\"," + timeout + ");\n"
            + " Anthem_InvokePageMethod( '_Refresh', [], null );"
            + "}\n"
            + "// -->\n"
            + "</script>\n"
            ;

//  " function(result) { document.getElementById('test').innerHTML = result.value; } );\n"
//          Anthem.Manager.AddScriptForClientSideEval( refreshFunction );

         ClientScript.RegisterClientScriptBlock( GetType(),
                                                 "doRefresh", 
                                                 refreshFunction );

         string refreshScript = 
            "<script>\n"
            + " <!--\n"
            + "setTimeout(\"doRefresh()\"," + timeout + ");\n"
            + "// -->\n"
            + "</script>\n"
            ;
      
         ClientScript.RegisterStartupScript( GetType(),
                                             "RefreshInit", 
                                             refreshScript );
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
            Credentials credentials = new Credentials();
            Mood mood = new Mood();
            
            backend.GetCurrentUserAndMood( ref credentials, ref mood );

            // Save what we THINK is the current state for later.
            this.credentials = credentials;
            this.mood = mood;
            
            _UpdateNowPlayingInfo( backend.GetState() );

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
      /// Stores/retrieves the current user's credentials from viewstate
      ///
      Credentials credentials
      {
         get
         {
            if (null == ViewState["userIdKey"])
            {
               throw new ApplicationException( 
                  "userIdKey is missing from view state" );
            }

            uint userId = (uint)ViewState["userIdKey"];

            // The text is really unused on the server side and for
            // validation, so there's no need to really check it:
            return new Credentials( userNameBtn.Text, userId );
         }

         set
         {
            userNameBtn.Text = value.name;
            userNameBtn.UpdateAfterCallBack = true;
            ViewState["userIdKey"] = value.id;
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
         
         nowAlbum.Text = current.artist;
         nowAlbum.UpdateAfterCallBack = true;
         
         nowFileName.Text = current.filePath;
         nowFileName.UpdateAfterCallBack = true;

         double suckPercent;
         double moodPercent;
         backend.GetAttributes( this.credentials,
                                this.mood,
                                current.key,
                                out suckPercent,
                                out moodPercent );

         suckPercent /= 100;
         nowSuckLevel.Text = ((int)suckPercent).ToString();
         nowSuckLevel.UpdateAfterCallBack = true;

         moodPercent /= 100;
         nowMoodLevel.Text = ((int)moodPercent).ToString();
         nowMoodLevel.UpdateAfterCallBack = true;
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
         try
         {
            // Hmm.
         }
         catch (Exception)
         {
         }
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
            backend.IncreaseSuckZenoStyle( this.credentials, 
                                           this.currentTrack );

            // Reevaluate this track based on its new suck level, if
            // it is currently playing.
            //
            // TODO: the engine should really be controlling when this
            // does or does not happen.
            //
            if (engineState.currentTrack.key == this.currentTrack)
               backend.ReevaluateCurrentTrack();

            // So: things have changed:
            _Refresh();
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
            backend.DecreaseSuckZenoStyle( this.credentials, 
                                           this.currentTrack );

            _Refresh();
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
            backend.IncreaseAppropriateZenoStyle( this.credentials, 
                                                  this.mood,
                                                  this.currentTrack );

            _Refresh();
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
            backend.DecreaseAppropriateZenoStyle( this.credentials, 
                                                  this.mood,
                                                  this.currentTrack );

            _Refresh();
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
            backend.GotoPrevFile( this.credentials, this.currentTrack );

            _Refresh();
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
            backend.GotoNextFile( this.credentials, this.currentTrack );
            _Refresh();
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
            _Refresh();
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
            _Refresh();
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
            throw;
         }
      }


   }
}
