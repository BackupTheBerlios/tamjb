/// \file
/// $Id$
///
/// Classes to wrap the selection of the next file "randomly".
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
// Feel free to track down and contact ALL project contributors to
// negotiate other terms. Bring a checkbook.
//
//   Tom Surace <tekhedd@byteheaven.net>

namespace tam
{
   using System;
   using System.Diagnostics;    // Debug.Assert()
   using tam.LocalFileDatabase;

   public class PlaylistEmptyException : Exception
   {
      public PlaylistEmptyException( string desc )
         : base( desc )
      {

      }
   }

   /// 
   /// Selects the next file to play. 
   ///
   /// \todo needs to know about the
   ///   internals of the database for efficiency, but we can work on that.
   ///
   public class FileSelector
   {
      public FileSelector( StatusDatabase db,
                           PlaylistCriteria criteria )
      {
         // Passed us a null reference parameter?
         Debug.Assert( null != db );
         Debug.Assert( null != criteria ); 
            
         _database = db;
         _criteria = criteria;
      }

      public void SetCriteria( PlaylistCriteria criteria )
      {
         _criteria = criteria;
      }

      ///
      /// Starts the engine playing by selecting the next song.
      ///
      /// \return full path to next track to play.
      ///
      public PlayableData ChooseNextSong()
      {
         _criteria.Randomize( _myRng );

         // For now, simply pick a random song using our one
         // criterion.
         uint nextKey;
         uint count = _database.PickRandom( ref _criteria, out nextKey );

         if (0 == count)
            throw new PlaylistEmptyException( "Playlist is empty" );

         return _database.GetFileInfo( nextKey );
      }

      StatusDatabase   _database;
      PlaylistCriteria _criteria;
      Random _myRng = new Random(); // They're all the rage.
   }
}
