/// \file
/// $Id$

// Copyright (C) 2004 Tom Surace.
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

         // Note: using mod here screws up the distribution if the 
         // range is not (exactly) a power of 2. Also, it wastes
         // the upper random bytes, especailly wasteful for range = 2, etc.
         return Next() % range;
      }

      public override int Next()
      {
         if (null == _urandom)
            return base.Next();

         // Just ignore the high bit--always returns a positive number
         return _binReader.ReadInt32() & 0x7fffffff;
      }

      BinaryReader _binReader;
      FileStream _urandom;
   }
}
