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
   /// References : Posted by mistert[AT]inwind[DOT]it
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

         Console.WriteLine( "w:" + w );
         Console.WriteLine( "n:" + n );
         Console.WriteLine( "a0:" + _a0 );
         Console.WriteLine( "a1:" + _a1 );
         Console.WriteLine( "b1:" + _b1 );
      }

      public double Process( double input )
      {
         _prevOutput = (input * _a0)
            + (_prevInput * _a1)
            + (_prevOutput * _b1)
            ;

         _prevInput = input;
         return _prevOutput;
      }

      double _a0 = 0.0; 
      double _a1 = 0.0; 
      double _b1 = 0.0; 
      double _prevInput = 0.0;  
      double _prevOutput = 0.0;  
   }

   ///
   /// This implementation is based on Bram's 
   /// lowpass from www.musicdsp.org, which just uses
   /// exponential decay. The cool part of this is the code to calculate
   /// the decay coefficient, which works nicely for lower frequencies.
   /// Unfortunately, it's not very high order. :o
   ///
   /// \code 
   /// References : Posted by Bram
   /// recursion: tmp = (1-p)*in + p*tmp with output = tmp
   /// coefficient: p = (2-cos(x)) - sqrt((2-cos(x))^2 - 1) 
   ///   with x = 2*pi*cutoff/samplerate
   /// coeficient approximation: p = (1 - 2*cutoff/samplerate)^2
   ///
   /// note: in recursion, tmp starts as the prev output.
   /// \endcode
   ///
   public class BramsLowpass
   {
      public void Initialize( double cutoff,
                              double sampleRate )
      {
         _prev = 0.0;

         //   with x = 2*pi*cutoff/samplerate
         double x = 2.0 * 3.14159265 * cutoff / sampleRate;

         // coefficient: p = (2-cos(x)) - sqrt((2-cos(x))^2 - 1) 
         _co = (2.0-Math.Cos(x))
            - Math.Sqrt( Math.Pow( (2.0-Math.Cos(x)), 2) - 1.0 );
         
         Debug.Assert( _co <= 1.0 && _co >= 0.0,
                       "Should not be inverting or amplifying!" );
         
         // Alternate method:
         // coeficient approximation: p = (1 - 2*cutoff/samplerate)^2
      }

      public double Process( double input )
      {
         //  with output = tmp
         _prev = ((1.0 - _co) * input) + (_co * _prev);
         return _prev;
      }
      
      double _co = 0.0;       // coeffecient a
      double _prev = 0.0;        // previous output
   }
}
