/// \file
///
/// $Id$
/// Header for the entire block of ID3v2 info
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

   ///
   /// A helper class that represents the header
   ///
   public class ID3v2Header
   {
      ///
      /// Size of the header info, in bytes
      ///
      public static readonly int SIZE_BYTES = 6;

      public bool isValid 
      {
         get
         {
            return _isValid;
         }
      }

      //
      // ID3 tag version. We may be hosed if this is 
      // not 4. 
      //
      public int version;

      /// 
      /// Retrieves the raw flags bits. Use the other booleans to access
      /// individual bits.
      ///
      public int flags = 0;

      ///
      /// This bit indicates that unsynchronization is used on all frames
      ///
      public bool isUnsynchronized
      {
         get
         {
            return 0 != (flags & 0x80); // 1 << 7
         }
      }

      public bool hasExtendedHeader
      {
         get
         {
            return 0 != (flags & 0x40); // 1 << 6
         }
      }

      public bool isExperimental
      {
         get
         {
            return 0 != (flags & 0x20); // 1 << 6
         }
      }

      public bool hasFooter
      {
         get
         {
            return 0 != (flags & 0x10); // 1 << 4
         }
      }

      ///
      /// Size of that there ID3 tag info
      ///
      /// "The ID3v2 tag size is the sum of the byte length of the extended
      /// header, the padding and the frames after unsynchronisation. If a
      /// footer is present this equals to ('total size' - 20) bytes, otherwise
      /// ('total size' - 10) bytes."
      ///
      public int size = 0; 

      ///
      /// \param buffer is assumed to contain 10 bytes that might be 
      ///   an id3 tag header
      /// \param startOfFile if false, look for the 3DI tag instead
      ///
      public ID3v2Header( byte [] buffer, bool startOfFile )
      {
         Debug.Assert( buffer.Length >= SIZE_BYTES );

         _isValid = false;
         if ((startOfFile && (buffer[0] == 'I' &&
                              buffer[1] == 'D' && 
                              buffer[2] == '3'))
             ||
             (!startOfFile && (buffer[0] == '3' && 
                               buffer[1] == 'D' && 
                               buffer[2] == 'I'))
             )
         {
            // Well, this is promising. Read the rest (we hope)
            //   ID3v2/file identifier      "ID3"
            //   ID3v2 version              $04 00
            //   ID3v2 flags                %abcd0000
            //   ID3v2 size             4 * %0xxxxxxx
            version = buffer[3];
            if (0xff == version)
            {
               throw new ApplicationException(
                  "Invalid version in ID3 header"
                  );
            }
            flags = buffer[5];
            size = ID3v2.DecodeSyncsafeInt( buffer, 6 );

            _isValid = true;
         }
      }
      bool _isValid ;
   }
}
