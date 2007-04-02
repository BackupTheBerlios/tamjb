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
   using ASP = System.Web.UI.WebControls;

   using Anthem;
   using byteheaven.tamjb.Interfaces;

   public class moodselect : byteheaven.tamjb.webgui.WebPageBase
   {
      protected Anthem.Repeater moodSelect;
      protected ASP.Literal currentUserBox;
      protected TextBox newMoodBox;

      override protected void OnLoad( EventArgs loadArgs )
      {
         base.OnLoad( loadArgs );

         try
         {
            Manager.Register( this );

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
            if (!Anthem.Manager.IsCallBack || Page.IsPostBack)
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
         Credentials currentUser = new Credentials();
         Mood currentMood = new Mood();
         backend.GetCurrentUserAndMood( ref currentUser,
                                        ref currentMood );

         currentUserBox.Text = currentUser.name;

         DataTable table = new DataTable();
         table.Columns.Add("moodKey", typeof(uint));
         table.Columns.Add("moodName", typeof(string));
         table.Columns.Add("status", typeof(string));

         foreach (Mood mood in backend.GetMoodList( currentUser ))
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
         moodSelect.UpdateAfterCallBack = true;
      }

      ///
      /// Get current credentials? Sloppy. Must deal with logins soon...
      ///
      Credentials _GetCredentials()
      {
         Credentials credentials = new Credentials();
         Mood currentMood = new Mood();
         backend.GetCurrentUserAndMood( ref credentials,
                                        ref currentMood );

         return credentials;
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
         Credentials credentials = _GetCredentials();
         // TODO: keep our credentials in the session or something, and then
         // don't bother with "RenewLogon".

         backend.RenewLogon( credentials ); // Why?

         uint newMood = Convert.ToUInt32( moodString );

         Console.WriteLine( "NewMood: {0}:{1}", newMood, moodString );

         // SetMood should take just the integer ID, not force creation
         // of a nameless object. :)
         Mood mood = new Mood( "", newMood );
         backend.SetMood( credentials, mood );
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

         Credentials credentials = _GetCredentials();

         // I really don't understand this:
         backend.RenewLogon( credentials ); // Why?

         Mood newMood = backend.CreateMood( credentials, newMoodBox.Text );
         backend.SetMood( credentials, newMood );

         newMoodBox.Text = "";
         newMoodBox.UpdateAfterCallBack = true;
         moodSelect.UpdateAfterCallBack = true;
      }

   }
}
