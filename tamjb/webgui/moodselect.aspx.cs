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
   using System.Data;
   using System.Web;
   using System.Web.UI.WebControls;

   using Anthem;
   using byteheaven.tamjb.Interfaces;

   public class moodselect : byteheaven.tamjb.webgui.WebPageBase
   {
      protected Anthem.Repeater moodSelect;
      protected Literal currentUserBox;

      override protected void OnLoad( EventArgs loadArgs )
      {
         base.OnLoad( loadArgs );

         try
         {
            Manager.Register( this );

            Console.WriteLine( "OL: {0}, {1}",
                               IsPostBack,
                               Anthem.Manager.IsCallBack );

            moodSelect.ItemCommand += 
               new RepeaterCommandEventHandler( _OnMoodCommand );

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

      public void _OnMoodCommand( object sender,
                                  RepeaterCommandEventArgs args )
      {
         Console.WriteLine( "OnMoodCommand {0}, {1}",
                            args.CommandName,
                            args.CommandArgument );

         Credentials credentials = new Credentials();
         Mood currentMood = new Mood();
         backend.GetCurrentUserAndMood( ref credentials,
                                        ref currentMood );

         if (credentials.name != currentUserBox.Text)
         {
            // TODO: report error here: current user changed in the
            // back end while we were working!
            _Refresh();
            return;
         }
         
         switch (args.CommandName)
         {
         case "select":
            backend.RenewLogon( credentials ); // Why?
            
            uint newMood = Convert.ToUInt32( args.CommandArgument );
            Mood mood = new Mood( "", newMood );
            backend.SetMood( credentials, mood );
            _Refresh();
            break;

         default:
            throw new ApplicationException( "Unexpected command" );

         }
      }
                                     

//       protected void _OnSelect( object sender, EventArgs ea )
//       {
//          if (null != QueryString["mood"])
//          {
//             string mood = (string)QueryString["mood"];
//             Console.WriteLine( "MOOD {0}", mood );
//          }
//       }

   }
}
