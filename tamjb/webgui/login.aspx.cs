/// \file
/// $Id: moodselect.aspx.cs 200 2007-04-02 22:00:04Z tekhedd $
///

// Copyright (C) 2007-2007 Tom Surace.
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


using byteheaven.tamjb.Interfaces;

namespace byteheaven.tamjb.webgui
{
   using System;
   using System.Collections;
   using System.Web;
   using System.Web.Security;
   using ASP = System.Web.UI.WebControls;
   
   using Anthem;
   
   public class login : byteheaven.tamjb.webgui.WebPageBase
   {
      protected ASP.Panel errorBox;
      protected ASP.TextBox idBox;
      protected ASP.TextBox passwordBox;
      protected ASP.Label errorMsg;

      override protected void OnLoad( EventArgs loadArgs )
      {
         base.OnLoad( loadArgs );


         // A hack to make anthem errors less cryptic
         if (Request.QueryString["Anthem_Callback"] == "true")
         {
            Anthem.Manager.Register(this);

            string alertScript = String.Format(
               "alert('Your session has timed out.');\n"
               + "window.location='{0}';",
               ResolveUrl( "~/" ) 
               );
                  
            Anthem.Manager.AddScriptForClientSideEval( alertScript );
         }


         if (! Page.IsPostBack)
         {
            errorBox.Visible = false;
         }
         else
         {
            string id = idBox.Text;
            string password = passwordBox.Text;

            // If no id was supplied, don't bail.
            if ("" == id)
               return;

            UserInfo userInfo = backend.LogIn( id, password );
            if (null != userInfo)
            {
               // Store the numeric id as the forms "user id"
               FormsAuthentication.RedirectFromLoginPage( userInfo.id.ToString(), false );
            }
            else
            {
               _Error( "Access Denied" );
            }
         }
      }

      void _Error( string msg )
      {
         errorBox.Visible = true;
         errorMsg.Text = msg;
      }
   }
}