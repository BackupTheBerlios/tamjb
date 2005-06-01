/// \file
/// $Id$

// Copyright (C) 2004-2005 Tom Surace.
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
   /// A class that tries to use urandom to generate random numbers,
   /// but uses the System.Random thingy as a fallback.
   ///
   public class MyRandom 
      : Random
   {

      public MyRandom() 
      {
         // If we can't open /dev/urandom, don't worry about it
         try
         {
            _urandom = new FileStream( "/dev/urandom", 
                                       FileMode.Open,
                                       FileAccess.Read );

            _binReader = new BinaryReader( _urandom );
         }
         catch
         {
            Console.WriteLine( "Note: Could not open /dev/urandom" );

            if (_urandom != null)
               _urandom.Close();

            _urandom = null;
            _binReader = null;
         }
      }

      public override int Next( int min, int max )
      {
         int range = max - min;

         return Next(range) + min;
      }

      public override int Next( int range )
      {
         if (null == _urandom)
            return base.Next( range );

         // how many bits are in the range? 
         int bits = CountBits( range );

         // Throw out bits higher than the range
         int mask = 0;
         for (int i = 0; i < bits; i++)
         {
            mask <<= 1;
            mask |= 0x00000001;
         }

         // Trace.WriteLine( "RANGE: " + range );
         // Trace.WriteLine( "BITS:  " + bits );
         // Trace.WriteLine( "MASK:  " + mask.ToString( "X8" ) );

         while (true)           // loop forever (until "return")
         {
            int next = Next();
            
            // Trace.WriteLine( "Next(1) " + next.ToString( "X8" ) );

            next &= mask;          // remove high bits

            // Trace.WriteLine( "Next(2) " + next.ToString( "X8" ) );
            
            // There's about a 25% chance the number will be larger than
            // requested. To retain linear probability, we have to try again
            // here:
            if (next < range)      // small enough?
               return next;        // ** success ** quick exit **
            
            // Trace.WriteLine( "Trying again..." );
         }
      }

      public override int Next()
      {
         if (null == _urandom)
            return base.Next();

         // Just ignore the high bit--always returns a positive number
         return _binReader.ReadInt32() & 0x7fffffff;
      }

      ///
      /// Count the significant bits
      ///
      int CountBits( int number )
      {
         // The trivial, slow, iterative method. There's probably a standard
         // api for doing this. So, in the meantime, I'll use the slow 
         // method that won't need much debugging. Note that this counts
         // the number of signifcant bits, not the number of SET bits. :)
         //
         // See: http://www-db.stanford.edu/~manku/bitcount/bitcount.html
         // 
         int nBits = 0;
         while (number != 0)
         {
            ++ nBits;
            number >>= 1;
         }
         return nBits;
      }

      BinaryReader _binReader;
      FileStream _urandom;
   }
}
