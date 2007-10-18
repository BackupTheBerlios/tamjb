/// \file
/// $Id: Credentials.cs 194 2007-02-13 22:34:44Z tekhedd $
///

// Copyright (C) 2004-2007 Tom Surace.
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

   ///
   /// User information and preferences. Serializable so that front ends 
   /// (such as the web front end) have this info and stuff.
   ///
   [Serializable]
   public class UserInfo
   {
      public enum Role
      {
         UNKNOWN,               ///< Error: invalid value (0). Do not use.
         CAT,                   ///< Controller account. Dominant. Jealous.
         MOUSE,                 ///< When the cat's away, the MICE will play
         NOBODY,                ///< Powerless bystander
      }

      ///
      /// default constructor for creating uninitialized instances
      ///
      public UserInfo()
      {
         name = "(unknown)";
         id = 0;
         role = Role.NOBODY;
      }

      public UserInfo( string name, uint id )
      {
         this.name = name;
         this.id = id;
         role = Role.NOBODY;
      }

      ///
      /// Non-reference assignment operator.
      ///
      public void Copy( UserInfo other )
      {
         name = other.name;
         id = other.id;
         role = other.role;
      }

      public uint id;
      public string name;
      public Role role;
      // public Preferences preferences;

   }
}
