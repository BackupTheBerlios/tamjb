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
   /// One pole lowpass.
   /// 
   /// From http://www.musicdsp.org/. Posted by mistert[AT]inwind[DOT]it.
   ///
   public class FirstOrderLowpassFilter
      : IMonoFilter
   {
      public void Initialize( double cutoff,
                              double sampleRate )
      {
         _prevInput = 0.0;
         _prevOutput = 0.0;
         
         double w = 2.0 * sampleRate; // nyquist freq?
         double twoPiCutoff = cutoff * 2.0 * 3.14159265;
         double n = 1.0 / (twoPiCutoff + w);

         _a0 = twoPiCutoff * n;
         _a1 = _a0;
         _b1 = (w - twoPiCutoff) * n;

//          Console.WriteLine( "w:" + w );
//          Console.WriteLine( "n:" + n );
//          Console.WriteLine( "a0:" + _a0 );
//          Console.WriteLine( "a1:" + _a1 );
//          Console.WriteLine( "b1:" + _b1 );
      }

      public double Process( double input )
      {
         // Introduces a high frequency hiss that should be inaudable
         // most of the time. Probably.

         _prevOutput = 
            (input * _a0)
            + (_prevInput * _a1)
            + (_prevOutput * _b1)
            + _denormalOffset
            ;

#if WATCH_DENORMALS
         Denormal.CheckDenormal( "FOLP a0", _a0 );
         Denormal.CheckDenormal( "FOLP a1", _a1 );
         Denormal.CheckDenormal( "FOLP b1", _b1 );
         Denormal.CheckDenormal( "FOLP output", _prevOutput );
#endif

         _prevInput = input;
         _denormalOffset = - _denormalOffset;
         return _prevOutput;
      }


      double _a0 = 1.0e-25; 
      double _a1 = 1.0e-25; 
      double _b1 = 1.0e-25; 
      double _prevInput = 0.0;
      double _prevOutput = 0.0;  

      // Should help prevent long stretches of 0's from causing performance
      // problems.
      double _denormalOffset = Denormal.denormalFixValue;

   }
}
