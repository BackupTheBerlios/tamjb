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
   using System.Web.Security;
   using System.Web.UI.WebControls;

   ///
   /// A class to hold JSON requests to a data store ala Dojo.
   /// 
   public class DojoStoreRequest
   {
      public class QueryOptions
      {
         public bool ignoreCase;
      }

      public string query;
      public QueryOptions queryOptions;
      public IDictionary sort;   // attribute, descending
      public int start;
      public int count;
   }
}