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
   /// ID3v2 frame format (from the informal spec:
   ///   Frame ID      $xx xx xx  (three characters)
   ///   Size      3 * %xx xx xx    (24-bit number in three bytes msb first)
   ///   (content)
   ///
   /// Size does not include size of header!
   ///
   public class ID3v2_2FrameHeader
      : ID3v2FrameHeader
   {
      public static readonly int SIZE_BYTES = 6;

      public bool isValid
      {
         get
         {
            return _isValid;
         }
      }

      public int size
      {
         get
         {
            return _size;
         }
      }

      ///
      /// Is this frame compressed (zlib deflate method)
      ///
      public bool isCompressed
      {
         get
         {
            return false;
         }
      }

      ///
      /// Remember, compression before encryption!
      ///
      public bool isEncrypted
      {
         get
         {
            return false;
         }
      }

      public bool isUnsynchronized
      {
         get
         {
            // But be sure to check the header, the whole tag may be
            // unsynchronized
            return false;
         }
      }

      public bool hasDataLength
      {
         get
         {
            return false;
         }
      }

      public string frameId
      {
         get
         {
            return _frameId;
         }
      }

      ///
      /// Construct the header from a buffer read from a stream.
      ///
      /// \param buffer is assumed to be at least 10 bytes in size
      ///
      public ID3v2_2FrameHeader( byte [] buffer )
      {
         Debug.Assert( buffer.Length >= SIZE_BYTES );

         _isValid = false;

         _frameId = _DecodeFrameId( buffer );
         if (_frameId.Length < 3) // found a null or invalid char
            return;
         
         // Size is stored as the next three bytes (a 24-bit msb int)
         _size = (((int)buffer[3]   << 16)
                  | ((int)buffer[4] << 8)
                  | ((int)buffer[5] << 0)
                  );

         if (_size <= 0)        // invalid size. Eek!
            return;

         _isValid = true; 
      }

      string _DecodeFrameId( byte [] buffer )
      {
         StringBuilder builder = new StringBuilder();

         builder.Append( (char)buffer[0] );
         builder.Append( (char)buffer[1] );
         builder.Append( (char)buffer[2] );

         return builder.ToString();
      }

      bool _isValid = false;
      int _size = 0;
      string _frameId = "";
   }
}
