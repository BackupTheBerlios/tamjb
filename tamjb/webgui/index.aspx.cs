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

   using Anthem;

   public class index : byteheaven.tamjb.webgui.WebPageBase
   {
      protected Anthem.Label nowTitle;
      protected Anthem.Label nowArtist;

      protected Anthem.Button refreshButton;

      override protected void OnLoad( EventArgs loadArgs )
      {
         base.OnLoad( loadArgs );

         try
         {
            if (! IsPostBack)
            {
               _InstallRefreshScript();
            }
         }
         catch (Exception)
         {
            // do nothing
         }
      }

      void _InstallRefreshScript()
      {
         string refreshScript = 
            "<script>\n"
            + " <!--\n"
            + "setTimeout(\""
            + ClientScript.GetPostBackEventReference( 
               this, refreshButton.ID.ToString() )
            + "')\",15000);\n"
            + "// -->\n"
            + "</script>\n"
            ;
      
         ClientScript.RegisterStartupScript( typeof(string),
                                             "AutoRefresh", 
                                             refreshScript );
      }

      ///
      /// Refresh all now-playing related display:
      ///
      protected void _OnRefresh( object sender, EventArgs ea )
      {
         nowTitle.Text = "test";
         nowTitle.UpdateAfterCallBack = true;

         nowArtist.Text = "artist";
         nowArtist.UpdateAfterCallBack = true;
      }

   }
}
