/// \file
///
/// $Id$
/// The byteheaven.id3 tag reader namespace primary entry point.
///

// Copyright (C) 2004 Tom Surace.
//
// This file is part of the byteheaven.id3 package.
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

namespace byteheaven.id3
{
   using System;
   using System.Diagnostics;
   using System.IO;
   using System.Text;
   using System.Collections;
   using System.Collections.Specialized;

   ///
   /// An id3 tag manipulation package, specifically targeted at reading
   /// information useful for playing audio files.
   ///
   /// This version is based on the ID3v2.4 specification.
   ///
   public class ID3v2
   {
      public bool isValid
      {
         ///
         /// \return true if an id3v2 header was found, false otherwise.
         ///
         /// \note Check other fields for null, there may have been no
         ///   useful data in the id3 block!
         ///
         get
         {
            // If the header was found, this is valid.
            return (null != _header);
         }
      }
      
      public ID3v2Header header
      {
         get
         {
            return _header;
         }
      }

      // Generic text fields (don't need to be read-only)
      // if NULL, these are not present in the file.d
      
      public string tcon = null;
      
      /// 
      /// Permits readonly access to the default genre (first in list)
      ///
      public string DefaultGenre
      {
         get
         {
            // Sorry, Genre parsing is still broken...I think I have to
            // get each char one at a time and compare to NULL. :(
            return null;
         }
      }

      public string tit2 = null;
      public string tpe1 = null;
      public string talb = null;
      public string trck
      {
         get
         {
            if (0 == trackIndex)
               return null;

            if (trackCount > 0)
            {
               return trackIndex.ToString() + "/" 
                  + trackCount.ToString();
            }
             
            return trackIndex.ToString();
         }
      }

      ///
      /// \note comment may contain newlines
      ///
      public string comm = null;

      public string tyer = null; // year?

      ///
      /// CD identifier. Yay!
      ///
      public byte [] mcdi = null;

      ///
      /// The actual track number of this song. If 0, not set.
      /// 
      public int trackIndex = 0;

      /// 
      /// Count of tracks in the album
      public int trackCount = 0;

      ///
      /// Reads a subset of the id3 information
      ///
      public ID3v2( string path )
      {
         _stream = new FileStream( path, FileMode.Open );
         _reader = new Mp3StreamReader( _stream );
         try
         {
            // Find the start of the ID3v2 tag in our stream:
            _header = _FindHeader( false );

            // For now, forget about it
            _SkipExtendedHeader(); // skip to the meat

            // Finally, start looking for interesting information in the
            // stream.
            _ReadFrames();

         }
         finally
         {
            _reader = null;

            _stream.Close();
            _stream = null;
         }
      }

      void _ReadFrames()
      {
         if (_header.isUnsynchronized)
         {
            throw new ApplicationException( 
               "TODO: unsynchronization not implemented yet" );
         }

         byte [] headerBuffer = new byte[ID3v2FrameHeader.SIZE_BYTES];

         // Keep reading frames until they're all gone
         while (true)
         {
            if (_reader.Position >=
                ((long)_header.size + ID3v2FrameHeader.SIZE_BYTES))
            {
               break;           // Out of ID3 data
            }

            // Console.WriteLine( "POSITION: {0}", _reader.Position );

            _reader.Read( headerBuffer, ID3v2FrameHeader.SIZE_BYTES );
            ID3v2FrameHeader frameHeader = 
               new ID3v2FrameHeader( headerBuffer );

            if (!frameHeader.isValid) // reached the end of the frames, I guess
               break;

            // Limit the size of a frame to something "reasonable"
            if (frameHeader.size > 1000000)
            {
               // skip frame
               Console.WriteLine( "Warning: frame is unusually large: " 
                                  + frameHeader.size );
               Console.WriteLine( " (skipping frame)" );
               _reader.Skip( frameHeader.size );
               continue;        // **NEXT FRAME**
            }

            // Read the frame contents, pass it to the handler
            byte [] contentBuffer = new byte[ frameHeader.size ];
            _reader.Read( contentBuffer, frameHeader.size );
            try
            {
               _OnFoundFrame( frameHeader, contentBuffer );
            }
            catch (Exception e)
            {
               Console.WriteLine( "Problem reading frame ({0})",
                                  frameHeader.frameId );
               // Just keep going
            }
         }
      }

      ///
      /// Called whent the parser finds a frame. 
      ///
      void _OnFoundFrame( ID3v2FrameHeader frameHeader,
                          byte [] contentBuffer )
      {
         if (frameHeader.isUnsynchronized)
         {
            Console.WriteLine( "Fixme: unsynchronization not implemented" );
            Console.WriteLine( " (skipping frame)" );
            return;
         }

         if (frameHeader.isEncrypted)
         {
            // skip frame
            Console.WriteLine( "Note: Skipping encrypted frame" );
            return;
         }

         if (frameHeader.isCompressed)
         {
            Console.WriteLine( "Fixme: Skipping compressed frame" );
            return;
         }

         switch (frameHeader.frameId)
         {
            // Some things I think might be useful if they turn up
         case "RVA2":           // Relative volume adjustment
         case "EQU2":           // Relative eq adjustment
         case "TBPM":           // bpm
         case "TCOM":           // composer (useful for classical)
         case "TCOP":           // copyright (yeah, I do care)
         case "TLEN":           // Length in...milliseconds
         case "TPE2":           // performed-by (band/supporting artist)
         case "TPE3":           // performed-by (condustor, etc)
         case "TRSN":           // Internet radio station...
         case "TIT1":           // song category thingy
         case "TIT3":           // Song refinement (op3)
            Console.WriteLine( "Desirable frame not supported :( - {0} -", 
                               frameHeader.frameId );
            break;

         case "TYER":
            tyer = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         case "MCDI":           // Music CD identifier -- oh yeah.
            mcdi = contentBuffer; // binary data
            break;

         case "TCON":           // content type (ROCK/Classical)
            tcon = _DecodeTextFrame( contentBuffer, frameHeader.size );
            // Important--get all values from null-separated list here.
            break;

         case "TIT2":           // song title
            tit2 = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         case "TPE1":           // performed-by (lead performer)
            tpe1 = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         case "TALB":           // album title
            talb = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         case "TRCK":           // track number
            string TRCK = _DecodeTextFrame( contentBuffer, frameHeader.size );
            string [] parts = TRCK.Split( '/' );
            if (parts.Length > 0)
               trackIndex = Convert.ToInt32( parts[0] );

            if (parts.Length > 1)
               trackCount = Convert.ToInt32( parts[1] );
               
            break;

         case "COMM":           // tag comment (ID3v1 style)
            comm = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         default:
            Console.WriteLine( "ID3 frame type not supported: - {0} -", 
                               frameHeader.frameId );
            break;
         }
      }

      ///
      /// Helper that retrieves the text value of a TXXX type frame.
      ///
      /// \param buffer contains the content after the type header.
      ///
      string _DecodeTextFrame( byte [] buffer, int length )
      {
         // All the text information frames have the following
         // format:
         // 
         // <Header for 'Text information frame', ID: "T000" - "TZZZ",
         // excluding "TXXX" described in 4.2.6.>
         // Text encoding                $xx
         // Information                  <text string(s) according to encoding>

         // Frames that allow different types of text encoding contains a text
         // encoding description byte. Possible encodings:
         //
         // $00   ISO-8859-1 [ISO-8859-1]. Terminated with $00.
         // $01   UTF-16 [UTF-16] encoded Unicode [UNICODE] with BOM. All
         //       strings in the same frame SHALL have the same byteorder.
         //       Terminated with $00 00.
         // $02   UTF-16BE [UTF-16] encoded Unicode [UNICODE] without BOM.
         //       Terminated with $00 00.
         // $03   UTF-8 [UTF-8] encoded Unicode [UNICODE]. Terminated with $00.

         Encoding enc;
         switch ((int)buffer[0])
         {
         case 0:                // iso-8859-1
            enc = Encoding.GetEncoding( "ISO-8859-1" );
            break;

         case 1:                // utf-16
            enc = Encoding.GetEncoding( "UTF-16" );
            break;

         case 2:                // utf-16be
            enc = Encoding.GetEncoding( "UTF-16BE" );
            break;

         case 3:                // utf-8
            enc = Encoding.GetEncoding( "UTF-8" );
            break;

         default:
            throw new ApplicationException(
               "Unexpected encoding type in frame: " + buffer[0] );
         }

         // Convert the bytes to a string using the encoding. Whee!
         ///
         /// \todo Parse out the multiple null-terminated strings in this
         /// block separately (must use an encoding-specific value of
         /// null!
         ///
         return enc.GetString( buffer, 1, length - 1 );
      }


      ~ID3v2()
      {
         if (null != _stream)
            _stream.Close();
      }

      ///
      /// \param tryTheEnd - if false, we only look in the first 3 bytes
      ///   for a header.
      ///
      ID3v2Header _FindHeader( bool tryTheEnd )
      {
         // look for the tag at the start or end of the file. If it's
         // somewhere in the middle, forget it.

         /// \bug We should be ready to deal with id3 tags in the middle
         ///   of the file, but this isn't very likely, so...

         // From the FAQ:
         // # Q: Where is an ID3v2 tag located in an MP3 file?
         //   It is most likely located at the beginning of the file. 
         //
         //   Look for the marker "ID3" in the first 3 bytes of the file.
         //
         //   If it's not there, it could be at the end of the file 
         //   (if the tag is ID3v2.4). Look for the marker "3DI" 10 
         //   bytes from the end of the file, or 10 bytes before the 
         //   beginning of an ID3v1 tag.
         //
         //   Finally it is possible to embed ID3v2 tags in the actual 
         //   MPEG stream, on an MPEG frame boundry. Almost nobody does 
         //   this.
         // 

         // Try the start
         byte [] buffer = new byte[10];

         _reader.Read( buffer, 10 ); // get the file header

         ID3v2Header header = new ID3v2Header( buffer, true );
         if (header.isValid)
            return header;

         if (!tryTheEnd)
         {
            /// \todo search for id3v2 tag in the middle of the file
            ///

            throw new ID3TagNotFoundException();
         }

         // Seek to end of file (new for 2.4)
         Console.WriteLine( "FIXME: look for ID3v2.4 tag at EOF" );
         throw new ID3TagNotFoundException();
      }

      ///
      /// Skips past the extended header, if present. Assumes that
      /// the current position is on the first byte after the 
      /// normal header
      ///
      void _SkipExtendedHeader()
      {
         // if we got here, it's assumed we parsed the header successfully
         Debug.Assert( null != _header );
         
         // Just get the size and ignore it
         if (_header.hasExtendedHeader)
         {
            byte [] buffer = new byte[ID3v2Header.SIZE_BYTES];
            _reader.Read( buffer, ID3v2Header.SIZE_BYTES );

            int extHeaderSize = DecodeSyncsafeInt( buffer, 0 );

            if (extHeaderSize < 6)
            {
               throw new ApplicationException( 
                  "Extended header size is less than 6 bytes: " 
                  + extHeaderSize.ToString()
                  );
            }

            // We've already read the first 6 bytes, skip the rest
            // of the ext header:
            _reader.Skip( extHeaderSize - 6 );
         }
      }

      /// 
      /// Decode the next 4 bytes into a (less than 32-bit sized) int.
      ///
      /// Yes, this assumes buffer contains 4 bytes.
      ///
      public static int DecodeSyncsafeInt( byte [] buffer, int offset )
      {
         Debug.Assert( buffer.Length >= (4 + offset) );

         // Note big-endian-ness of id3 stream
         int decoded = (((int)buffer[offset + 0] << 21)
                        | ((int)buffer[offset + 1] << 14)
                        | ((int)buffer[offset + 2] << 7)
                        | ((int)buffer[offset + 3] )
                        );

         return decoded;
      }

      // The file or other device that contains the id3 data
      FileStream _stream;
      Mp3StreamReader _reader;

      //
      // Things found in the mp3 file.
      //
      ID3v2Header _header = null;
   }
}
