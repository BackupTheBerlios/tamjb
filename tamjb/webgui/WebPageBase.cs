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
   using System.Configuration;
   using System.Web;

   using byteheaven.tamjb.Interfaces; // IEngine

   ///
   /// Base class for interface web pages
   ///
   public class WebPageBase : System.Web.UI.Page
   {

      protected string serverUrl
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
      protected IEngine backend
      {
         get
         {
            return (IEngine) Activator.GetObject( typeof(IEngine), 
                                                  this.serverUrl );
         }
      }
   }
}
