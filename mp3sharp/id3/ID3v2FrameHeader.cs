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
   ///
   /// An interface for generically accessing parts of ID3v2 headers.
   /// 
   /// Older versions are represented using features of the newer headers.
   ///
   public interface ID3v2FrameHeader
   {
      bool isValid { get; }
      int size { get; }

      ///
      /// Is this frame compressed? (zlib deflate method)
      ///
      bool isCompressed { get; }

      ///
      /// Remember, compression before encryption!
      ///
      bool isEncrypted { get; }

      bool isUnsynchronized { get; }

      ///
      /// What _is_ this good for, anyway
      ///
      bool hasDataLength { get; }

      ///
      /// Frame ID is 4 chars long for id3v2.4, 3 chars for v2.2
      ///
      string frameId { get; }
   }
}
