/// \file
/// $Id$

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
   using System.Configuration;
   using System.IO;

   using byteheaven.tamjb.Interfaces;

   public class StereoCrossover 
   {
      ///
      /// \note Assumes sample rate of 44100
      ///
      public StereoCrossover( double cutoff )
      {
         // Define the bass/mid FIR as fairly poor quality, because
         // otherwise the filter has a lot of coefficients and your
         // poor Pentium III will roll over and play dead.
         double beta;
         double [] lowpass = KaiserWindow.FromParameters
            ( cutoff / 2.0 / 22050.0,
              cutoff * 4.0 / 22050.0,
              45,               // A (60 is good, 50 acceptable, 70 is GREAT)
              400,              // maxM
              out beta );

         _lowFilter = new DualFIR( lowpass );

         // Left and right ought to be the same
         int delaySize = _lowFilter.minDelaySize;

         _delayLineLeft = new double[ delaySize ];
         _delayLineRight = new double[ delaySize ];

         _lowFilter.delayLineLeft = _delayLineLeft;
         _lowFilter.delayLineRight = _delayLineRight;

         // The Kaiser window (linear) introduces an M/2 sample delay, so set 
         // up a delay line tap at that point for the midrange. A positive
         // starting offset into the buffer is a positive delay...
         _midTapOffset = 
            _lowFilter.delayLineOffset - (lowpass.Length / 2);

         if (_midTapOffset < 0)
            _midTapOffset += lowpass.Length;
      }

      ///
      /// Implmenets IAudioProcessor.Process.
      ///
      public void Process( double left, double right,
                           out double leftLowOut, out double rightLowOut,
                           out double leftHiOut, out double rightHiOut )
      {
         leftLowOut = left;
         rightLowOut = right;
         _lowFilter.Process( ref leftLowOut, ref rightLowOut );

         leftHiOut = _delayLineLeft[ _midTapOffset ] - leftLowOut;
         rightHiOut = _delayLineRight[ _midTapOffset ] - rightLowOut;

         ++ _midTapOffset;
         if (_midTapOffset >= _delayLineLeft.Length)
            _midTapOffset = 0;

#if WATCH_DENORMALS
         Denormal.CheckDenormal( "SC leftlow", leftLowOut );
         Denormal.CheckDenormal( "SC lefthi", leftHiOut );
         Denormal.CheckDenormal( "SC rightlow", rightLowOut );
         Denormal.CheckDenormal( "SC righthi", rightHiOut );
#endif
      }

      // Filter for the low freq crossover
      DualFIR _lowFilter;

      int _midTapOffset;
      double [] _delayLineLeft;
      double [] _delayLineRight;
   }

}