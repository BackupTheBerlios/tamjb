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
// Feel free to track down and contact ALL project contributors to
// negotiate other terms. Bring a checkbook.
//
//   Tom Surace <tekhedd@byteheaven.net>

namespace tam.LocalFileDatabase
{
   using System;

   ///
   /// Information about the state of a local,
   /// playable file, or a references to remote streams.
   ///
   /// For example, if an mp3 file is on the local system, it will
   /// be listed here (if the scanner, etc are configured to search
   /// for it). If it later turns up to be missing or is found to be
   /// corrupt, that info will be added here.
   ///
   [Serializable]
   public class PlayableData : ITrackInfo
   {
      ///
      /// unique database key. Ignored on adds and so on.
      uint _key;
      public uint   key
      {
         get
         {
            return _key;
         }
         set
         {
            _key = value;
         }
      }

      /// Path at which the file may be found. 
      ///
      string _filePath;
      public string filePath
      {
         get
         {
            return _filePath;
         }
         set
         {
            _filePath = value;
         }
      }

      /// Artist, album, and title together form a unique index by 
      /// which this may be located. If any is empty, this is an error.
      /// Use "Unknown", "Various", or other sensible strings. (Can
      /// match the freedb strings to make this easier.)
      string _artist;
      public string artist
      {
         get
         {
            return _artist;
         }
         set
         {
            _artist = value;
         }
      }
      
      /// \see artist
      ///
      string _album;
      public string album
      {
         get
         {
            return _album;
         }
         set
         {
            _album = value;
         }
      }

      /// \see artist
      ///
      string _title;
      public string title
      {
         get
         {
            return _title;
         }
         set
         {
            _title = value;
         }
      }

      ///
      /// Track index (indicating play order in album).
      ///
      int _track;
      public int track
      {
         get
         {
            return _track;
         }
         set
         {
            _track = value;
         }
      }

      ///
      /// This is the genre discovered from ID3 or other file info. It
      /// may not be the same as other database info and certainly is
      /// not very useful after that has been changed.
      ///
      /// \todo initial discovery info like this could be in a different
      ///   table and disposed of after it is no longer useful.
      ///
      string _genre;
      public string genre
      {
         get
         {
            return _genre;
         }
         set
         {
            _genre = value;
         }
      }

      /// Length of the track. Longer tracks may be given less priority
      /// in some scheduling schemes (so that individual tracks play more
      /// frequently than, say, mix tapes).
      long _lengthInSeconds;
      public long   lengthInSeconds
      {
         get
         {
            return _lengthInSeconds;
         }
         set
         {
            _lengthInSeconds = value;
         }
      }

   };

}
