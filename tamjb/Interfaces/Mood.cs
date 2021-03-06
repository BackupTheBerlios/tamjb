/// \file
/// $Id$
///

// Copyright (C) 2004 Tom Surace.
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
// Contacts:
//
//   Tom Surace <tekhedd@byteheaven.net>

namespace byteheaven.tamjb.Interfaces
{
   using System;
   using byteheaven.tamjb.Interfaces;

   [Serializable]
   public class Mood
   {
      public string name 
      { 
         get
         {
            return _name;
         }
         set
         {
            _name = value;
         }
      }

      public uint id 
      { 
         get
         {
            return _id;
         }
         set
         {
            _id = value;
         }
      }

      ///
      /// default constructor for creating uninitialized instances
      ///
      public Mood()
      {
         _name = "(unknown)";
         _id = 0;
      }

      public Mood( string name, uint id )
      {
         _name = name;
         _id = id;
      }
      
      public void Copy( Mood other )
      {
         _name = other._name;
         _id = other._id;
      }

      string _name;
      uint _id;
   }
}

