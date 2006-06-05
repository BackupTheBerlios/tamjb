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
      protected Literal           userIdKey;
      protected Anthem.LinkButton userNameBtn;
      protected Anthem.LinkButton moodBtn;

      protected Literal      nowTrackKey; // hidden track ID key
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
            + " Anthem_InvokePageMethod( '_Refresh', [], "
            + " function(result) { document.getElementById('test').innerHTML = result.value; } );\n"
            + "}\n"
            + "// -->\n"
            + "</script>\n"
            ;

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
            EngineState engineState = backend.GetState();

            Credentials credentials = new Credentials();
            Mood mood = new Mood();
            
            backend.GetCurrentUserAndMood( ref credentials, ref mood );
            
            userNameBtn.Text = credentials.name;
            userNameBtn.UpdateAfterCallBack = true;
            
            moodBtn.Text = mood.name;
            userNameBtn.UpdateAfterCallBack = true;
            
            _UpdateNowPlayingInfo( engineState );

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

      void _UpdateNowPlayingInfo( EngineState engineState )
      {
         ITrackInfo current = engineState.currentTrack;

         nowTitle.Text = current.title;
         nowTitle.UpdateAfterCallBack = true;

         nowTrackKey.Text = current.key.ToString();
         
         nowArtist.Text = current.artist;
         nowArtist.UpdateAfterCallBack = true;
         
         nowAlbum.Text = current.artist;
         nowAlbum.UpdateAfterCallBack = true;
         
         nowFileName.Text = current.filePath;
         nowFileName.UpdateAfterCallBack = true;
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

      protected void _OnSuck( object sender, EventArgs ea )
      {
         try
         {
            EngineState engineState = backend.GetState();

            // The engine could be rewound past 0 or whatever by some
            // other user.
            if (engineState.currentTrackIndex < 0)
               return;

            // What is the track showing in the GUI? This is the one we want
            // to flag as sucking.

            uint suckTrackKey;
            try
            {
               suckTrackKey = Convert.ToUInt32( nowTrackKey.Text );
            }
            catch (Exception e)
            {
               throw new ApplicationException( 
                  "now playing track key is invalid: " + nowTrackKey.Text,
                  e);
            }

            // Who are we? Right now the program trusts us. :)
            uint userId;
            try
            {
               userId = Convert.ToUInt32( userIdKey.Text );
            }
            catch (Exception e2)
            {
               throw new ApplicationException( 
                  "user credentials id is invalid: " + userIdKey.Text,
                  e2 );
            }

            Credentials credentials = new Credentials( userNameBtn.Text,
                                                       userId );

            // Flag the previously playing track as suck
            backend.IncreaseSuckZenoStyle( credentials, suckTrackKey );

            // Reevaluate this track based on its new suck level, if
            // it is currently playing.
            //
            // TODO: the engine should really be controlling when this
            // does or does not happen.
            //
            if (engineState.currentTrack.key == suckTrackKey)
               backend.ReevaluateCurrentTrack();

            // So: things have changed:
            engineState = backend.GetState();
            _UpdateNowPlayingInfo( engineState );
         }
         catch (Exception)
         {
            throw;
         }
      }

      protected void _OnRule( object sender, EventArgs ea )
      {
         try
         {
            // Hmm.
         }
         catch (Exception)
         {
            throw;
         }
      }

      protected void _OnYes( object sender, EventArgs ea )
      {
         try
         {
            // Hmm.
         }
         catch (Exception)
         {
            throw;
         }
      }

      protected void _OnNo( object sender, EventArgs ea )
      {
         try
         {
            // Hmm.
         }
         catch (Exception)
         {
            throw;
         }
      }

   }
}
