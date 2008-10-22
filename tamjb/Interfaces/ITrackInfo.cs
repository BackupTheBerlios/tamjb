/// \file
/// $Id$
///
/// Track Information interface for remoting.
///

// Copyright (C) 2004-2008 Tom Surace.
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
   public enum TrackEvaluation
   {
      ALL_GOOD,              ///< Track is fine to play
      SUCK_TOO_MUCH,         ///< Rejected because it sucks
      WRONG_MOOD             ///< Rejected because not in the mood
   }

   /// 
   /// Track's physical status, as in missing or damaged
   ///
   public enum TrackStatus : int
   {
      OK,
      MISSING
   }

   public interface ITrackInfo
   {
      ///
      /// unique database key. Ignored on adds and so on.
      ///
      uint   key{ get; }

      /// Path at which the file may be found. 
      ///
      string filePath{ get; }

      /// Artist, album, and title together form a unique index by 
      /// which this may be located. If any is empty, this is an error.
      /// Use "Unknown", "Various", or other sensible strings. (Can
      /// match the freedb strings to make this easier.)
      string artist{ get; }
      
      /// \see artist
      ///
      string album{ get; }

      /// \see artist
      ///
      string title{ get; }

      ///
      /// Track index (indicating play order in album).
      ///
      int track{ get; }

      ///
      /// This is the genre discovered from ID3 or other file info. It
      /// may not be the same as other database info and certainly is
      /// not very useful after that has been changed.
      ///
      /// \todo initial discovery info like this could be in a different
      ///   table and disposed of after it is no longer useful.
      ///
      string genre{ get; }

      /// Length of the track. Longer tracks may be given less priority
      /// in some scheduling schemes (so that individual tracks play more
      /// frequently than, say, mix tapes).
      long   lengthInSeconds{ get; }

      ///
      /// The possible reasons why a track is being played or not played
      /// by the system.
      ///
      TrackEvaluation evaluation{ get; }

      TrackStatus status{ get; }

      ///
      /// How many times has the track been played?
      ///
      uint playCount{ get; }
   }
}
