// $Id$
//

namespace tam
{
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

   }
}
