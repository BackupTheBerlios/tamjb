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
   /// An FIR filter engine
   ///
   public class FIR 
   {

      ///
      /// Accessor to allow using the delay line by other
      /// processes. 
      ///
      public double [] delayLine;

      ///
      /// Offset of the current time in the delay line. New samples are at
      /// smaller offsets. So, the previous sample is at delayOffset - 1.
      /// 
      public int  delayLineOffset
      {
         get
         {
            return _buffOffset;
         }
      }

      ///
      /// Construct a filter using the given coefficients.
      /// 
      public FIR( double [] points )
      {
         // Double-wide coefficient array!
         _coef = new double[ points.Length * 2 ];

         int j = points.Length;
         for (int i = 0; i < points.Length; i++)
         {
            _coef[i] = points[i];
            _coef[j] = points[i];
            ++ j;

#if WATCH_DENORMALS
            Denormal.CheckDenormal( "FIR coefficient", points[i] );
#endif
         }


         // Buffer for incoming samples. Delay is therefore the length
         // of this buffer PLUS the filter's delay time.
         delayLine = new double[points.Length];

         _offset = 0;

         // Goes in the opposite direction from _offset:
         _buffOffset = delayLine.Length - 1;
      }

      public double Process( double input )
      {
         // do multiply/accumumlate processing.
         // Use offsets within the coefficient array instead of 
         // shifting all the data. Whee.
         
         // Save the input as the new "youngest" buffered sample
//          Debug.Assert( _buffOffset == delayLine.Length - _offset - 1,
//                        "buffer and coefficient offsets are out of sync "
//                        + _buffOffset + ":" + _offset  + ":" + delayLine.Length );

         delayLine[ _buffOffset ] = input;

         // For large buffers, the denormal fix could cause a large
         // DC offset? Probably not: 700 is a LARGE buffer, and 
         // 700 * 1.0e-25 is only 7e-23. So...not much of a problem if
         // we only invert it after each buffer.

         double output = 0.0;
         int j = _offset;
         for (int i = 0; i < delayLine.Length; i++)
         {
            output += (delayLine[i] * _coef[j]) + _denormalFix;
            ++ j;

#if WATCH_DENORMALS
            Denormal.CheckDenormal( "FIR", output );
#endif
         }

         _denormalFix = - _denormalFix;

         // Shift the buffer left, or...shift the coefficients to
         // the right. Same thing really. Fake circular buffers. :/

         if (_offset <= 0)
         {
            _offset = delayLine.Length - 1;
            _buffOffset = 0;
         }
         else
         {
            -- _offset;
            ++ _buffOffset;
         }

         return output;
      }

      double [] _coef;
      public double [] buff;
      int _offset = 0;          // current offset into _coef
      int _buffOffset = 0;      // current offset into _buff

      double _denormalFix = Denormal.denormalFixValue;
   }
}
