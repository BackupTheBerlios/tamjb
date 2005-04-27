/// \file
/// $Id$
///

// Copyright (C) 2005 Tom Surace.
//
// This file is part of the Tam Jukebox project.
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

namespace byteheaven.tamjb.Engine
{
   using System;
   using System.Diagnostics;
   using System.IO;

   ///
   /// A short delay that works as a fifo. For long integers.
   ///
   public class SampleFifo
   {
      ///
      /// \param maxSize is the size length of the fifo in samples. The delay
      ///    can be set to less than this size, but not greater. This must be
      ///    >= 1. (However, the delay may be set to 0, see below.)
      /// \param initialValue initial value with which the fifo will be filled.
      ///
      public SampleFifo( uint maxSize, long initialValue )
      {
         Debug.Assert( maxSize > 0, "Bad parameter" );

         // Create and initialize the buffer to 0's

         _buffer = new long[ maxSize ];
         for (int i = 0; i < maxSize; i++)
            _buffer[i] = initialValue;

         _bufptr = 0;           // offset within _buffer
         _delay = maxSize;      // default - use the whole buffer
      }

      ///
      /// Amount of delay to introduce. Must be >= 0 and <= the size
      /// of this fifo. It may be set to 0, in which case the delay
      /// is effectively disabled (but somewhat inefficient, if you 
      /// get my drift).
      ///
      public uint delay
      {
         set
         {
            Debug.Assert( value <= _buffer.Length, 
                          "delay larger than buffer" );
            Debug.Assert( value >= 0, "delay  may not be negative" );

            if (value > _buffer.Length)
               _delay = (uint)_buffer.Length;
            else if (value < 0)
               _delay = 0;
            else
               _delay = value;
         }
         get
         {
            return _delay;
         }
      }

      ///
      /// Puts the new value on the top of the stack, returning
      /// the value from the bottom. Simple
      ///
      public long Push( long newValue )
      {
         if (0 == _delay)       // No delay!
            return newValue;

         // Advance to the next sample, ring buffer style. Really,
         // is this all there is to it?
         ++ _bufptr;
         if (_bufptr >= _delay)
            _bufptr = 0;

         long prev = _buffer[ _bufptr ];
         _buffer[ _bufptr ] = newValue;
         return prev;
      }

      long [] _buffer;
      uint    _bufptr;
      uint    _delay;
   }
}
