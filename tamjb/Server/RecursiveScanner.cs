// $Id$
// tam server

namespace tam.Server
{
   using System;
   using System.Diagnostics;
   using System.IO;
   using mp3IdkonvertUtil;
   using tam.LocalFileDatabase;

   enum ScanStatus
   {
      NOT_FINISHED,
      FINISHED,
   }

   ///
   /// Object that manages importing mp3 files from a directory tree
   /// in the background.
   ///
   class RecursiveScanner
   {
      /// 
      /// ID3 genre lookup table. 
      /// \todo Move this to a separate ID3 class (either a subclass of our id3
      ///   library or whatever.
      ///
      static readonly string [] ID3_GENRE_TABLE = {
         "Blues", "Classic Rock", "Country", "Dance",
         "Disco", "Funk", "Grunge", "Hip-Hop",
         "Jazz", "Metal", "New Age", "Oldies",
         "Other", "Pop", "R&B", "Rap", "Reggae",
         "Rock", "Techno", "Industrial", "Alternative",
         "Ska", "Death Metal", "Pranks", "Soundtrack",
         "Euro-Techno", "Ambient", "Trip-Hop", "Vocal",
         "Jazz+Funk", "Fusion", "Trance", "Classical",
         "Instrumental", "Acid", "House", "Game",
         "Sound Clip", "Gospel", "Noise", "Alt",
         "Bass", "Soul", "Punk", "Space",
         "Meditative", "Instrumental Pop",
         "Instrumental Rock", "Ethnic", "Gothic",
         "Darkwave", "Techno-Industrial", "Electronic",
         "Pop-Folk", "Eurodance", "Dream",
         "Southern Rock", "Comedy", "Cult",
         "Gangsta Rap", "Top 40", "Christian Rap",
         "Pop/Funk", "Jungle", "Native American",
         "Cabaret", "New Wave", "Psychedelic", "Rave",
         "Showtunes", "Trailer", "Lo-Fi", "Tribal",
         "Acid Punk", "Acid Jazz", "Polka", "Retro",
         "Musical", "Rock & Roll", "Hard Rock", "Folk",
         "Folk/Rock", "National Folk", "Swing",
         "Fast-Fusion", "Bebob", "Latin", "Revival",
         "Celtic", "Bluegrass", "Avantgarde",
         "Gothic Rock", "Progressive Rock",
         "Psychedelic Rock", "Symphonic Rock", "Slow Rock",
         "Big Band", "Chorus", "Easy Listening",
         "Acoustic", "Humour", "Speech", "Chanson",
         "Opera", "Chamber Music", "Sonata", "Symphony",
         "Booty Bass", "Primus", "Porn Groove",
         "Satire", "Slow Jam", "Club", "Tango",
         "Samba", "Folklore", "Ballad", "Power Ballad",
         "Rhythmic Soul", "Freestyle", "Duet",
         "Punk Rock", "Drum Solo", "A Cappella",
         "Euro-House", "Dance Hall", "Goa",
         "Drum & Bass", "Club-House", "Hardcore",
         "Terror", "Indie", "BritPop", "Negerpunk",
         "Polsk Punk", "Beat", "Christian Gangsta Rap",
         "Heavy Metal", "Black Metal", "Crossover",
         "Contemporary Christian", "Christian Rock",
         "Merengue", "Salsa", "Thrash Metal",
         "Anime", "JPop", "Synthpop"
      };

      public RecursiveScanner( string rootDir, Engine engine )
      {
         Debug.Assert( engine != null, "bad parameter" );

         _engine = engine;
         _rootDir = rootDir;

         Rewind();
      }

      ///
      /// Resets the scanner to the first file
      ///
      public void Rewind()
      {
         _scanner =  new SubdirScanner( _rootDir );
      }

      public ScanStatus DoNextFile()
      {
         return this.DoNextFile( 1 );
      }

      ///
      /// Scan the files in one directory. This traverses the current
      /// directory scan state to the bottommost leaf and retrieves 
      /// the files from one directory, or the next "n" files, depending
      /// on whether I've implemented that yet.
      ///
      /// \return FINISHED if no more subdirs have files
      ///
      public ScanStatus DoNextFile( int nFiles )
      {
         string next;
         for (int i = 0; i < nFiles; i++)
         {
            if (! _scanner.GetNext( out next ))
               return ScanStatus.FINISHED;

            if (next.ToLower().EndsWith( "mp3"))
               _UpdateMP3FileInfo( next );
         }

         return ScanStatus.NOT_FINISHED;
      }

      ///
      /// Updates the database info for a file. Assumes the file is an
      /// MP3 file.
      ///
      /// \todo Create a pluggable interface to extract the file info
      ///   from various file types, not just mp3
      ///
      void _UpdateMP3FileInfo( string path )
      {
         // Get ID3 tags from the file

         CID3v1Tag tag = new CID3v1Tag( path );
         if (! tag.Read())
         {
            // Couldn't read this file's tag. Silently (?) skip.
            return;
         }
         
//          if (tag.Title.Length == 0) // strip whitespace first?
//          {
//             // Use the file basename? Or what?
//             // Console.WriteLine( "Missing info: " + path );
//          }

         string genre;
         if (tag.Genre < ID3_GENRE_TABLE.GetLength(0))
            genre = ID3_GENRE_TABLE[tag.Genre];
         else
            genre = "unknown";

         if (! _engine.EntryExists( path )) 
         {
            Trace.WriteLine( "add> " + path );

            PlayableData data = new PlayableData();
            data.filePath = path;
            data.artist = tag.Artist;
            data.album = tag.Album;
            data.title = tag.Title;
            data.track = tag.Track;
            data.genre = genre;
            data.lengthInSeconds = 0; // unknown, ID3 tag doesn't know
            _engine.Add( data );
         }
      }

      string         _rootDir;
      Engine         _engine;
      SubdirScanner  _scanner;
   }


   ///
   /// \todo Should the root parameter be absolute?
   ///
   /// \todo Handle other than just mp3 files
   ///
   class SubdirScanner
   {
      public SubdirScanner( string dir )
      {
         DirectoryInfo dirInfo = new DirectoryInfo( dir );

         // This array may be QUITE large, some people put all files
         // in one place. :/
         _files = dirInfo.GetFiles();
         _fileIndex = 0;

         _subdirs = Directory.GetDirectories( dir );
         _subdirIndex = 0;

         // Create first subdir, if any subdirs exist
         if (_subdirs.Length > 0) 
            _currentSubdir = new SubdirScanner( _subdirs[0] );
      }

      ///
      /// \param file is returned set to the next file from this
      ///   search, or not modified if there are no more files.
      ///
      /// \return false if no more files in this dir, and no more
      ///   subdirs to process
      ///
      public bool GetNext( out string file )
      {
         // Get the next file in this dir, or
         // get the next file from the current subdir, or
         // goto the next subdir
         // If no more subdirs, we're done

         if (_fileIndex < _files.Length)
         {
            file = _files[_fileIndex].FullName;
            ++ _fileIndex;
            return true;
         }
         else 
         {
            // Scan through subdirs until a file is found or no more
            // subdirs exist:

            if (_subdirIndex >= _subdirs.Length)
            {
               file = null;
               return false; // No more subdirs!
            }

            while (true)
            {
               Debug.Assert( null != _currentSubdir, "logic error" );

               if (_currentSubdir.GetNext( out file ))
                  return true;

               // If we got here, the current subdir is finished
               ++ _subdirIndex;
               if (_subdirIndex >= _subdirs.Length)
                  return false;

               _currentSubdir = new SubdirScanner( _subdirs[_subdirIndex] );
            }
         }

         // not reached
      }

      SubdirScanner _currentSubdir; // A child of this dir

      FileInfo []   _files;
      int           _fileIndex;
      string []     _subdirs;
      int           _subdirIndex;
   }

}
