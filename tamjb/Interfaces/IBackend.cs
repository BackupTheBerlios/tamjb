/// \file
/// $Id$
///
/// Interface to the internal backend interfaces
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
   ///
   /// Super secret internal interfaces to the backend that are
   /// exposed to local users of hte dll. Hmmm.
   ///
   public interface IBackend
   {
      bool EntryExists( string fullPath );
      void Add( PlayableData newData );
      void Poll();

      // Kills all player threads and such so the program can exit
      void ShutDown();

      int desiredQueueSize { get; set; }
      uint bufferSize{ get; set; }
      uint bufferCount{ get; set; }
      uint bufferPreload{ get; set; }

   }
}
