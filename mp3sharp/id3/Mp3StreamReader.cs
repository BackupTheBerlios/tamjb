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

   ///
   /// A helper class to retrieve bytes from the mpeg stream intelligently.
   ///
   public class Mp3StreamReader
   {
      public long Position
      {
         ///
         /// \return the current offset within the stream.
         ///
         get
         {
            return _stream.Position;
         }
      }

      public Mp3StreamReader( FileStream source )
      {
         Debug.Assert( null != source );

         _stream = source;
      }

      ///
      /// Read some bytes from the stream, raw.
      ///
      public void Read( byte [] bytes, int count )
      {
         int got = 0;
         while (got < count)
         {
            int gotBytes = _stream.Read( bytes, 0, count );
            if (gotBytes <= 0)
            {
               if (gotBytes < 0)
               {
                  // This doesn't happen?
                  Trace.WriteLine( 
                     "Warning: _stream.Read returned < 0!, ("
                     + gotBytes + ")", 
                     "AUD" );
               }

               throw new ApplicationException( 
                  "end of file reached while searching for id3 tag" );
            }
            got += gotBytes;
         }
      }

      ///
      /// Tells the reader to seek this far forward in bytes. Useful for
      /// skipping uninteresting blocks (that we already know the size of)
      ///
      public void Skip( int count )
      {
         Debug.Assert( count >= 0 );
         
         _stream.Seek( count, SeekOrigin.Current );
      }

      FileStream _stream;
   }

}
