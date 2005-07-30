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
   using System.Diagnostics;

   ///
   /// An FIR filter engine where the same coefficients are used on
   /// two streams of data.
   ///
   public class DualFIR 
   {
      public int minDelaySize
      {
         get
         {
            return _length;
         }
      }

      ///
      /// Construct a filter using the given coefficients.
      /// 
      public DualFIR( double [] points )
      {
         _length = points.Length;

         _coef = new double[ points.Length ];

         for (int i = 0; i < points.Length; i++)
         {
            _coef[i] = points[i];

#if WATCH_DENORMALS
            Denormal.CheckDenormal( "FIR coefficient", points[i] );
#endif
         }
      }

      public void Process( double [] leftDelayLine, 
                           double [] rightDelayLine,
                           int offset,
                           out double leftOutput,
                           out double rightOutput )
      {
         // do multiply/accumumlate processing. Initialize the accumulator
         // to 0:
         leftOutput = 0.0;
         rightOutput = 0.0;

         // For large buffers, the denormal fix could cause a large
         // DC offset? Probably not: 700 is a LARGE buffer, and 
         // 700 * 1.0e-25 is only 7e-23. So...not much of a problem if
         // we only invert it after each buffer.

         // Where does the buffer start? If it's less than 0, process
         // in two steps...
         int j = 0;             // coefficient buffer cuonter
         int i = offset - _length; // Find starting index
         if (i < 0)             // Wraps past start of array?
         {
            i = leftDelayLine.Length + i;
            while (i < leftDelayLine.Length)
            {
               leftOutput += (leftDelayLine[i] * _coef[j]) + _denormalFix;
               rightOutput += (rightDelayLine[i] * _coef[j]) + _denormalFix;
               ++ i;
               ++ j;

#if WATCH_DENORMALS
            Denormal.CheckDenormal( "FIR", leftOutput );
            Denormal.CheckDenormal( "FIR", rightOutput );
#endif
            }

            // Fix up the offset/range for the rest of the calculation
            i = 0;
         }

         while (j < _coef.Length)
         {
            leftOutput += (leftDelayLine[i] * _coef[j]) + _denormalFix;
            rightOutput += (rightDelayLine[i] * _coef[j]) + _denormalFix;
            ++ i;
            ++ j;

#if WATCH_DENORMALS
            Denormal.CheckDenormal( "FIR", leftOutput );
            Denormal.CheckDenormal( "FIR", rightOutput );
#endif
         }

         _denormalFix = - _denormalFix;
      }

      double [] _coef;
      int _length;              // Length of the FIR

      double _denormalFix = Denormal.denormalFixValue;
   }
}
