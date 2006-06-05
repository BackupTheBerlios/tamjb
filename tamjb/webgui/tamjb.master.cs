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

   public class master : System.Web.UI.MasterPage
   {
      override protected void OnLoad( EventArgs loadArgs )
      {
         base.OnLoad( loadArgs );

         Anthem.Manager.Register( this );

         try
         {
            if (! IsPostBack)
            {
               // Still do nothing
            }
         }
         catch (Exception)
         {
            // do nothing
         }
      }

   }
}
