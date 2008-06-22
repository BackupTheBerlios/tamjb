/// \file
/// $Id$
///

// Copyright (C) 2006-2008 Tom Surace.
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

   using byteheaven.tamjb.Interfaces;

   public class moodselect : byteheaven.tamjb.webgui.WebPageBase
   {
      protected Repeater moodSelect;
      protected Literal currentUserBox;
      protected TextBox newMoodBox;

      UserInfo _userInfo;

      override protected void OnLoad( EventArgs loadArgs )
      {
         base.OnLoad( loadArgs );

         try
         {
            System.Security.Principal.IIdentity identity = 
               HttpContext.Current.User.Identity;

            if (! identity.IsAuthenticated)
            {
               throw new ApplicationException( 
                  "Internal error, anonymous access not supported" );
            }
            uint userId = Convert.ToUInt32( identity.Name );
            _userInfo = backend.RenewLogon( userId );
            if (null == _userInfo)
            {
               FormsAuthentication.RedirectToLoginPage();
            }

            // Special: handle command line parameters. :)
            string action = (string)Request.Params["action"];
            if (null != action)
            {
               switch (action)
               {
               case "select":
                  string mood = (string)Request.Params["mood"];
                  if (null == mood)
                  {
                     throw new ArgumentException( "Parameter missing",
                                                  "mood" );
                  }
                  _SetMood( mood );
                  
		  // Note: Server.Transfer does not cause the Anthem javascript to
		  // render properly, so Anthem.AddEvent does not exist, and ajax 
		  // callbacks stop working. :(
		  
                  Response.Redirect( "~/index.aspx" );
                  break;        // not reached
               }                  
            }

            if (!IsPostBack)
            {
               _Refresh();
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
            // If the list needs to be updated on a callback, the 
            // callback function shall explicitly call _Refresh().
            if (Page.IsPostBack)
            {
               _Refresh();
            }
         }
         catch (Exception)
         {
            throw;              // not handled yet
         }
      }

      void _Refresh()
      {
         Mood currentMood = new Mood();
         backend.GetCurrentMood( _userInfo.id, ref currentMood );

         currentUserBox.Text = _userInfo.name;

         DataTable table = new DataTable();
         table.Columns.Add("moodKey", typeof(uint));
         table.Columns.Add("moodName", typeof(string));
         table.Columns.Add("status", typeof(string));

         foreach (Mood mood in backend.GetMoodList( _userInfo.id ))
         {
            DataRow row = table.NewRow();
            row["moodKey"] = mood.id;
            row["moodName"] = mood.name;

            if (mood.id == currentMood.id)
               row["status"] = "MOOD_CURRENT";
            else
               row["status"] = "MOOD_OTHER";

            table.Rows.Add(row);
         }

         moodSelect.DataSource = table;
         moodSelect.DataBind();  
      }

//       public void _OnMoodCommand( object sender,
//                                   ASP.RepeaterCommandEventArgs args )
//       {
//          Console.WriteLine( "OnMoodCommand {0}, {1}",
//                             args.CommandName,
//                             args.CommandArgument );

//          Credentials credentials = _GetCredentials();

//          if (credentials.name != currentUserBox.Text)
//          {
//             // TODO: report error here: current user changed in the
//             // back end while we were working!
//             _Refresh();
//             return;
//          }

//          switch (args.CommandName)
//          {
//          case "select":
//             backend.RenewLogon( credentials ); // Why?

//             uint newMood = Convert.ToUInt32( args.CommandArgument );
//             Mood mood = new Mood( "", newMood );

//             // SetMood should take just the integer ID.
//             backend.SetMood( credentials, mood );
//             // _Refresh();
//             Server.Transfer( "index.aspx" );
//             break;

//          default:
//             throw new ApplicationException( "Unexpected command" );

//          }
//       }

      void _SetMood( string moodString )
      {
         uint newMood = Convert.ToUInt32( moodString );

         Console.WriteLine( "NewMood: {0}:{1}", newMood, moodString );

         // SetMood should take just the integer ID, not force creation
         // of a nameless object. :)
         Mood mood = new Mood( "", newMood );
         backend.SetMood( _userInfo.id, mood.id );
      }
                                     

      ///
      /// So, violent mood swings, eh?
      ///
      protected void _OnCreate( object sender, EventArgs ea )
      {
         if (newMoodBox.Text.Length == 0)
         {
            return;
         }

         Mood newMood = backend.CreateMood( _userInfo.id, newMoodBox.Text );
         backend.SetMood( _userInfo.id, newMood.id );

         newMoodBox.Text = "";
      }

   }
}
