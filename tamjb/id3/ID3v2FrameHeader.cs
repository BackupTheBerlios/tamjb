/// \file
/// $Id$
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

   ///
   /// A class that represents (and parses out) one frame's header
   ///
   /// Frame ID      $xx xx xx xx  (four characters)
   /// Size      4 * %0xxxxxxx
   /// Flags         $xx xx
   ///
   public class ID3v2FrameHeader
   {
      public static readonly int SIZE_BYTES = 10;

      public bool isValid
      {
         get
         {
            return _isValid;
         }
      }

      public int size = 0;
      public byte flags1;
      public byte flags2;

      ///
      /// Is this frame compressed (zlib deflate method)
      ///
      public bool isCompressed
      {
         get
         {
            return 0 != (flags2 & 0x08);
         }
      }

      ///
      /// Remember, compression before encryption!
      ///
      public bool isEncrypted
      {
         get
         {
            return 0 != (flags2 & 0x04);
         }
      }

      public bool isUnsynchronized
      {
         get
         {
            return 0 != (flags2 & 0x02);
         }
      }

      public bool hasDataLength
      {
         get
         {
            return 0 != (flags2 & 0x01);
         }
      }

      public string frameId;

      ///
      /// Construct the header from a buffer read from a stream.
      ///
      /// \param buffer is assumed to be at least 10 bytes in size
      ///
      public ID3v2FrameHeader( byte [] buffer )
      {
         Debug.Assert( buffer.Length >= SIZE_BYTES );

         _isValid = false;

         frameId = _DecodeFrameId( buffer );
         if (frameId.Length < 4) // found a null or invalid char
            return;
         
         size = ID3v2.DecodeSyncsafeInt( buffer, 4 ); // size offset 4
         if (size <= 0)
            return;

         flags1 = buffer[8];
         flags2 = buffer[9];
         
         _isValid = true;
      }

      string _DecodeFrameId( byte [] buffer )
      {
         StringBuilder builder = new StringBuilder();

         builder.Append( (char)buffer[0] );
         builder.Append( (char)buffer[1] );
         builder.Append( (char)buffer[2] );
         builder.Append( (char)buffer[3] );

         return builder.ToString();
      }

      bool _isValid;
   }
}
