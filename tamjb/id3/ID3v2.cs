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
         _stream = new FileStream( path, 
                                   FileMode.Open,
                                   FileAccess.Read,
                                   FileShare.Read );
         _reader = new Mp3StreamReader( _stream );
         try
         {
            // Find the start of the ID3v2 tag in our stream:
            _header = _FindHeader( false );
            Debug.Assert( null != _header );

#if VERBOSE_DUMP
            _Trace( "  isValid: " + _header.isValid );
            _Trace( "  version: " + _header.version );
            _Trace( "  flags: " + _header.flags );
            _Trace( "  isUnsynchronized: " + _header.isUnsynchronized );
            _Trace( "  hasExtendedHeader: " + _header.hasExtendedHeader );
            _Trace( "  isExperimental: " + _header.isExperimental );
            _Trace( "  hasFooter: " + _header.hasFooter );
            _Trace( "  size: " + _header.size );
#endif

            // For now, forget about it
            _SkipExtendedHeader(); // skip to the meat

            // Finally, start looking for interesting information in the
            // stream.
            _ReadFrames();

         }
         catch (ID3TagNotFoundException nfe)
         {
            _header = null;     // not found. Boo!
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
         // Allocate a buffer for reading in the frame headers

         int frameHeaderSize;
         switch (_header.version)
         {
         case 2:
            frameHeaderSize = ID3v2_2FrameHeader.SIZE_BYTES;
            break;

         case 3:
         case 4:
            frameHeaderSize = ID3v2_4FrameHeader.SIZE_BYTES;
            break;

         default:
            throw new ApplicationException( 
               "Sorry, don't know what to do with ID3v"
               + _header.version
               + " frames!" );
         }

         byte [] headerBuffer = new byte[frameHeaderSize];

         // Keep reading frames until they're all gone
         while (true)
         {
            if (_reader.Position >= ((long)_header.size + frameHeaderSize))
            {
               break;           // Out of ID3 data
            }

#if VERBOSE_DUMP
            _Trace( "  Reading frame at position: " + _reader.Position );
#endif

            _reader.Read( headerBuffer, frameHeaderSize );

            ID3v2FrameHeader frameHeader;
            switch (_header.version)
            {
            case 2:
               frameHeader = new ID3v2_2FrameHeader( headerBuffer );
               break;
               
            case 3:
            case 4:
               frameHeader = new ID3v2_4FrameHeader( headerBuffer );
               break;

            default:
               throw new ApplicationException( "Unexpected case in switch" );
            }

            if (!frameHeader.isValid) // reached the end of the frames, I guess
               break;

            // Limit the size of a frame to something "reasonable"
            if (frameHeader.size > 1000000) // like, 1 million bytes? Right.
            {
               // skip frame
               _Trace( "Note: this '"
                       + frameHeader.frameId
                       + "' frame is unusually large: "
                       + frameHeader.size );

               _Trace( " (skipping frame)" );
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
               _Trace( "Problem reading frame '"
                       + frameHeader.frameId
                       + "'" );
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
#if VERBOSE_DUMP
         _Trace( "[_OnFoundFrame]" );
         _Trace( "  isValid: " + frameHeader.isValid );
         _Trace( "  size: " + frameHeader.size );
         _Trace( "  isCompressed: " + frameHeader.isCompressed );
         _Trace( "  isEncrypted: " + frameHeader.isEncrypted );
         _Trace( "  isUnsynchronized: " + frameHeader.isUnsynchronized );
         _Trace( "  hasDataLength: " + frameHeader.hasDataLength );
         _Trace( "  frameId: " + frameHeader.frameId );
#endif

         if (frameHeader.isEncrypted)
         {
            // skip frame
            _Trace( "Note: Skipping encrypted frame" );
            return;
         }

         if (frameHeader.isCompressed)
         {
            _Trace( "Fixme: Skipping compressed frame" );
            return;
         }

         // if either this, or ALL frames are unsynchronized, deal with it
         // "This bit MUST be set if the
         // frame was altered by the unsynchronisation and SHOULD NOT be set if
         // unaltered. If all frames in the tag are unsynchronised the
         // unsynchronisation flag in the tag header SHOULD be set. It MUST NOT
         // be set if the tag has a frame which is not unsynchronised."

         int contentSize;
         if (frameHeader.isUnsynchronized ||
             _header.isUnsynchronized)
         {
            _FixUnsynchronized( contentBuffer, 
                                frameHeader.size,
                                out contentSize );
         }
         else
         {
            contentSize = frameHeader.size;
         }

#if VERBOSE_DUMP
         _Trace( "Found Frame <"
                 + frameHeader.frameId
                 + ">" );
#endif

         switch (frameHeader.frameId)
         {
            // Some things I think might be useful if they turn up
         case "RVA2":           // Relative volume adjustment
         case "EQU2":           // Relative eq adjustment
         case "TBPM":           // bpm
         case "TBM":            // ""
         case "TCOM":           // composer (useful for classical)
         case "TCOP":           // copyright (yeah, I do care)
         case "TLEN":           // Length in...milliseconds
         case "TLE":            // ""
         case "TPE2":           // performed-by (band/supporting artist)
         case "TPE3":           // performed-by (condustor, etc)
         case "TRSN":           // Internet radio station...
         case "TIT1":           // song category thingy
         case "TIT3":           // Song refinement (op3)
            // Note: "GEOB" frames include a mime type and are frequently HUGE
//             _Trace( "Desirable frame not supported :( '"
//                     + frameHeader.frameId
//                     + "'" );
            break;

         case "TYER":
         case "TYE":
            tyer = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         case "MCDI":           // Music CD identifier -- oh yeah.
         case "MCI":
            mcdi = contentBuffer; // binary data
            break;

         case "TCON":           // content type (ROCK/Classical)
         case "TCO":            // content type (ROCK/Classical)
            tcon = _DecodeTextFrame( contentBuffer, frameHeader.size );
            // Important--get all values from null-separated list here.
            break;

         case "TIT2":           // song title
         case "TT2":
            tit2 = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         case "TPE1":           // performed-by (lead performer)
         case "TP1":            // performed-by (lead performer)
            tpe1 = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         case "TALB":           // album title
         case "TAL":            // album title
            talb = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

         case "TRCK":           // track number
         case "TRK":
            string TRCK = _DecodeTextFrame( contentBuffer, frameHeader.size );
            string [] parts = TRCK.Split( '/' );
            if (parts.Length > 0)
               trackIndex = Convert.ToInt32( parts[0] );

            if (parts.Length > 1)
               trackCount = Convert.ToInt32( parts[1] );
               
            break;

         case "COMM":           // tag comment (ID3v1 style)
         case "COM":
            comm = _DecodeTextFrame( contentBuffer, frameHeader.size );
            break;

            //
            // ID3v2.2 and earlier frames are decoded here!
            // 

         default:
//             _Trace( "ID3 frame type not supported: '"
//                     + frameHeader.frameId
//                     + "'" );
            break;
         }
      }

      ///
      /// Modifies buffer in-place, removing unsynchronization codes.
      ///
      /// "The only purpose of the 'unsychronisation scheme' is to make 
      /// the ID3v2 tag as compatible as possible with existing software
      /// [at the time the ID3v2.2 standard was drafted]"
      ///
      /// This is stupid! Obviously the mp3 format is a transport stream!
      /// Therefore, players should be able to find and use those all-
      /// important sync bytes, and a stupid tagging format that is not
      /// properly embedded in mpeg frames should make every effort to 
      /// avoid causing problems. Therefore, I think every ID3 tag program
      /// should unsynchronize frames as necessary to remove sync patterns!
      ///
      void _FixUnsynchronized( byte [] buffer, 
                               int length,
                               out int newLength )
      {
         _Trace( "[FixUnsynchronized]" );

         // And I quote:
         // "
         //    %11111111 111xxxxx
         //
         // and should be replaced with:
         // 
         //    %11111111 00000000 111xxxxx
         //
         // This has the side effect that all $FF 00 combinations have to be
         // altered, so they will not be affected by the decoding process.
         // Therefore all the $FF 00 combinations have to be replaced with the
         // $FF 00 00 combination during the unsynchronisation."
         //

         //
         // To reverse this, we look for 0xff 0x00, and replace the 0x00
         // by the next byte. Ah, the joy of transport streams!
         // 
         newLength = length;
         int offset = 0;        // offset of data produced by shortening

         byte prev = buffer[0]; // preload the first byte
         int left = 1;          // copy destination
         int right = 1;         // copy source
         while (true)
         {
            // An unsynchronization sequence MUST be three bytes long. Cool.
            if ((right + 1) >= length)
               break;           // all done

            // If the previous byte was an 0xff, and this byte is 0,
            // skip this byte, because it's padding!

            if ((prev == 0xff) && (buffer[right] == 0x00))
            {
               ++ right;
               -- newLength;
               prev = 0;        // next byte is guaranteed to be OK
            }
            else
            {
               prev = buffer[right];
            }

            // We simply recopy the buffer without the extra (stuffed)
            // zero's, but only after we find the first one!

            if (right > left)
               buffer[left] = buffer[right];

            ++left;
            ++right;
         }

         _Trace( "  Old Length: " + length );
         _Trace( "  New Length: " + length );
      }

      ///
      /// Helper (for the v2.3/4 frame classes) that retrieves the text 
      /// value of a TXXX or similar frame, using the appropriate encoding.
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
         {
#if VERBOSE_DUMP
            _Trace( "  Found header at start" );
#endif
            return header;
         }

         if (!tryTheEnd)
         {
            /// \todo search for id3v2 tag in the middle of the file
            ///

            throw new ID3TagNotFoundException();
         }

         // Seek to end of file (new for 2.4)
         _Trace( "FIXME: look for ID3v2.4 tag at EOF" );
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
            _Trace( "  Extended header found--skipping" );

            byte [] buffer = new byte[4];
            _reader.Read( buffer, 4 );
            int extHeaderSize = DecodeSyncsafeInt( buffer, 0 );

            if (extHeaderSize < 6)
            {
               throw new ApplicationException( 
                  "Extended header size is less than 6 bytes: " 
                  + extHeaderSize.ToString()
                  );
            }

            // We've already read the first 4 bytes (size), skip the rest
            // of the ext header too!
            _reader.Skip( extHeaderSize - 4 );
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

      static void _Trace( string msg )
      {
         Trace.WriteLine( msg, "ID3v2" );
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
