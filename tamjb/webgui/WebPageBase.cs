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
   using System.Configuration;
   using System.Web;
   using System.Web.UI.WebControls;

   using byteheaven.tamjb.Interfaces; // IEngine

   ///
   /// Base class for interface web pages
   ///
   public class WebPageBase : System.Web.UI.Page
   {
      // Used to optimize retrieval of the back end pointer in a single
      // call.
      private static IEngine _backend = null;

      protected static string serverUrl
      {
         get
         {
            string serverUrl = ConfigurationManager.AppSettings["ServerUrl"];
            if (null == serverUrl)
               throw new ApplicationException( "ServerUrl not configured" );

            return serverUrl;
         }
      }

      ///
      /// get a reference to the jukebox engine. Somehow.
      ///
      /// \todo: make this "internal" when the RPC services are compiled
      ///   into the same DLL.
      ///
      public static IEngine backend
      {
         get
         {
            if (null == _backend)
            {
               _backend = (IEngine) Activator.GetObject( typeof(IEngine), 
                                                         serverUrl );
            }

            return _backend;
         }
      }

      ///
      /// Helper that emits the proper javascript for setting focus.
      ///
      protected void SetFocusTo( WebControl control )
      {
         // Does dojo make this unnecessary?
//          Anthem.Manager.AddScriptForClientSideEval(
//             String.Format( "document.getElementById('{0}').focus();",
//                            control.ClientID ) );
      }


   }
}
