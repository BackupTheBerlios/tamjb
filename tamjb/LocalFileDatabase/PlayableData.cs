// $Id$

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
