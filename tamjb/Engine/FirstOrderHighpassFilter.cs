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
   /// First order highpass, with some interesting recursion. Works
   /// very nicely.
   /// 
   /// One pole first order.
   /// 
   /// From http://www.musicdsp.org/. Posted by mistert[AT]inwind[DOT]it.
   ///
   public class MonoHighpassFilter 
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

         _a0 = w * n;
         _a1 = - _a0;
         _b1 = (w - twoPiCutoff) * n;
      }

      public double Process( double input )
      {
         _prevOutput = (input * _a0)
            + (_prevInput * _a1)
            + (_prevOutput * _b1)
            + _denormalOffset
            ;


#if WATCH_DENORMALS
         Denormal.CheckDenormal( "FOHP a0", _a0 );
         Denormal.CheckDenormal( "FOHP a1", _a1 );
         Denormal.CheckDenormal( "FOHP b1", _b1 );
         Denormal.CheckDenormal( "FOHP output", _prevOutput );
#endif

         _prevInput = input;
         _denormalOffset = - _denormalOffset;
         return _prevOutput;
      }

      double _a0 = 0.0; 
      double _a1 = 0.0; 
      double _b1 = 0.0; 
      double _prevInput = 0.0;  
      double _prevOutput = 0.0;  

      double _denormalOffset = Denormal.denormalFixValue;
   }
}
