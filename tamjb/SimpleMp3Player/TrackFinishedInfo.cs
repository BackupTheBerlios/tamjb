/// \file
/// $Id$
///

namespace tam.SimpleMp3Player
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
         ERROR,                 ///< generic error, see enclosed exception
      }
         
      ///
      /// This constructor is used when the track finishes playing
      /// successfully by reaching the end of its audio.
      ///
      public TrackFinishedInfo( uint trackKey )
      {
         _key = trackKey;
         _reason = Reason.NORMAL;
         _whatWentWrong = null;
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
      public TrackFinishedInfo( uint trackKey, Exception e )
      {
         _key = trackKey;
         _reason = Reason.ERROR;
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
