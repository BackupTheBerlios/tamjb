/// \file
/// $Id$
///

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
