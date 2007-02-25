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
      /// Quality level settings, reflecting the tradeoff of CPU/ ram bandwidth
      /// vs. crossover quality.
      ///
      public enum Quality
      {
         LOW,
         MEDIUM,
         HIGH
      }

      ///
      /// \note Assumes sample rate of 44100
      ///
      public StereoCrossover( double lowCutoff, double highCutoff,
                              Quality qualityLevel )
      {
         // Quality of the kaiser window for the low-mid crossover.
         // A (60 is good, 50 acceptable, 70 is GREAT). 45 swamps my
         // PIII-600. If you have something faster you definitely should
         // increase this. (HIGH is quite acceptable, and none sound
	 // actually bad.)
         int lowCrossoverQuality;
         switch (qualityLevel)
         {
         case Quality.LOW:
            lowCrossoverQuality = 23;
            break;

         case Quality.MEDIUM:
            lowCrossoverQuality = 46;
            break;

         case Quality.HIGH:
            lowCrossoverQuality = 70;
            break;

         default:
            throw new ApplicationException( "unexpected case in switch" );
         }

         double beta;
         double [] lowFiltCoef = KaiserWindow.FromParameters
            ( lowCutoff / 2.0 / 22050.0,
              lowCutoff * 4.0 / 22050.0,
              lowCrossoverQuality,
              400,              // maxM
              out beta );

         _lowFilter = new DualFIR( lowFiltCoef );


         double highCut = highCutoff * 2.0 / 22050.0;
         if (highCut > 1.0)
            highCut = 1.0;

         double [] highFiltCoef = KaiserWindow.FromParameters
            ( highCutoff / 2.0 / 22050.0,
              highCut,
              50,               // A (60 is good, 50 acceptable, 70 is GREAT)
              100,              // maxM
              out beta );

         _highFilter = new DualFIR( highFiltCoef );


         // Left and right ought to be the same
         int delaySize = Math.Max( _lowFilter.minDelaySize,
                                   _highFilter.minDelaySize );
         

         _delayLineLeft = new double[ delaySize ];
         _delayLineRight = new double[ delaySize ];

         _offset = 0;

         // The Kaiser window (linear) introduces an M/2 sample delay, so set 
         // up a delay line tap at that point for the midrange. A positive
         // starting offset into the buffer is a positive delay...
         _midTapOffset = delaySize + 1 - (lowFiltCoef.Length / 2);

         Debug.Assert( _midTapOffset >= 0, "oops" );

         // Similarly, compensate for the delay in the (assumed to be 
         // smaller) highpass crossover. 
         Debug.Assert( highFiltCoef.Length < lowFiltCoef.Length,
                       "Why is the highpass filter larger?" );

         _highOffset = (delaySize + 1) // same as "0" in circular buffer
            - (lowFiltCoef.Length / 2) // minus lowfilter delay
            + (highFiltCoef.Length / 2); //  plus highfilter delay

         Debug.Assert( _highOffset >= 0, "oops" );
      }

      ///
      /// \note the inputs are passed by reference for performance,
      ///   they are not modified
      ///
      public void Process( ref double left, ref double right,
                           out double leftLowOut, out double rightLowOut,
                           out double leftMidOut, out double rightMidOut,
                           out double leftHighOut, out double rightHighOut )
      {
         _delayLineLeft[ _offset ] = left;
         _delayLineRight[ _offset ] = right;

         _lowFilter.Process( _delayLineLeft, 
                             _delayLineRight, 
                             _offset, 
                             out leftLowOut,
                             out rightLowOut );

#if WATCH_DENORMALS
         Denormal.CheckDenormal( "SC leftlow", leftLowOut );
         Denormal.CheckDenormal( "SC lefthi", leftHiOut );
         Denormal.CheckDenormal( "SC rightlow", rightLowOut );
         Denormal.CheckDenormal( "SC righthi", rightHiOut );
#endif

         // Choose an offset to delay the high filter by the same
         // amount as the mid and low:
         _highFilter.Process( _delayLineLeft, 
                              _delayLineRight, 
                              _highOffset, 
                              out leftHighOut,
                              out rightHighOut );
         
         // Mid = highcrossoverout - basscrossoverout
         // Treble = fullrange - highcrossoverout
         // Bass = basscrossoverout

         leftMidOut = leftHighOut - leftLowOut;
         rightMidOut = rightHighOut - rightLowOut;

         leftHighOut = _delayLineLeft[ _midTapOffset ] - leftHighOut;
         rightHighOut = _delayLineRight[ _midTapOffset ] - rightHighOut;

         ++ _midTapOffset;
         if (_midTapOffset >= _delayLineLeft.Length)
            _midTapOffset = 0;

         ++ _highOffset;
         if (_highOffset >= _delayLineLeft.Length)
            _highOffset = 0;

         ++ _offset;
         if (_offset >= _delayLineLeft.Length)
            _offset = 0;
      }

      // Filter for the low freq crossover
      DualFIR _lowFilter;
      DualFIR _highFilter;

      double [] _delayLineLeft;
      double [] _delayLineRight;

      // Current offset within the delay line
      int _offset = 0;

      // Offset of the delay introduced by the fir's
      int _midTapOffset;

      // Offset of the high crossover (for delay compensation)
      int _highOffset;
   }

}
