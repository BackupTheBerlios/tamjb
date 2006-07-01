/// \file
/// $Id$
///
/// Info returned when a track stops playing. Possibly obsolete.
///

// Copyright (C) 2004-2006 Tom Surace.
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
//   Tom Surace <tekhedd@byteheaven.net>


namespace byteheaven.tamjb.SimpleMp3Player
{
   using System;
   using System.IO;
   using System.Diagnostics;

   ///
   /// Sent to listeners when the mp3 player is finished playing
   /// a track (or cannot play it because of error).
   ///
   public class TrackFinishedInfo
   {
      ///
      /// Reason that the track is no longer playing.
      ///
      public enum Reason : int
      {
         NORMAL,                ///< reached end of file or whatever
         USER_REQUEST,          ///< like normal, but you asked for it

            ///
            /// There was a problem opening the file (probably missing, but
            /// maybe an mp3 decoder constructor problem).
            ///
            OPEN_ERROR,

            ///
            /// There was a problem while playing the file
            ///
            PLAY_ERROR,                 
      }
         
      ///
      /// This constructor allows you to explicitly state the status.
      /// But does not allow you to set the exception!
      ///
      public TrackFinishedInfo( uint trackKey, Reason why )
      {
         _key = trackKey;
         _reason = why;
         _whatWentWrong = null;
      }

      ///
      /// This constructor implies that a generic error occurred, and is
      /// the reason the track is finished. An exception is attached.
      ///
      public TrackFinishedInfo( uint trackKey, 
                                Exception e,
                                Reason why )
      {
         _key = trackKey;
         _reason = why;
         _whatWentWrong = e;
      }

      public Reason reason
      {
         get
         {
            return _reason;
         }
      }

      public Exception exception
      {
         ///
         /// get the enclosed ecxeption.
         ///
         /// \warning May be null
         ///
         get
         {
            return _whatWentWrong;
         }
      }

      public uint key
      {
         get
         {
            return _key;
         }
      }

      uint      _key;           // track unique key.
      Reason    _reason;
      Exception _whatWentWrong;
   }
}
