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

      ///
      /// Accessor to allow using the delay line by other
      /// processes. 
      ///
      public double [] delayLineLeft;
      public double [] delayLineRight;

      public int minDelaySize
      {
         get
         {
            // The coefficient length is double
            return _coef.Length / 2;
         }
      }

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
      public DualFIR( double [] points )
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

         // In my filter, the delay pointer counts DOWN, and the
         // FIR offset counts UP. So time goes backward from normal, 
         // I guess.
         _offset = 0;

         // Goes in the opposite direction from _offset:
         _buffOffset = points.Length - 1;
      }

      public void Process( ref double leftInput, ref double rightInput )
      {
         // do multiply/accumumlate processing.
         // Use offsets within the coefficient array instead of 
         // shifting all the data. Whee.
         
         // Save the input as the new "youngest" buffered sample
//          Debug.Assert( _buffOffset == delayLine.Length - _offset - 1,
//                        "buffer and coefficient offsets are out of sync "
//                        + _buffOffset + ":" + _offset  + ":" + delayLine.Length );

         delayLineLeft[ _buffOffset ] = leftInput;
         delayLineRight[ _buffOffset ] = rightInput;

         // For large buffers, the denormal fix could cause a large
         // DC offset? Probably not: 700 is a LARGE buffer, and 
         // 700 * 1.0e-25 is only 7e-23. So...not much of a problem if
         // we only invert it after each buffer.

         double leftOutput = 0.0;
         double rightOutput = 0.0;
         int j = _offset;
         for (int i = 0; i < delayLineLeft.Length; i++)
         {
            leftOutput += (delayLineLeft[i] * _coef[j]) + _denormalFix;
            rightOutput += (delayLineRight[i] * _coef[j]) + _denormalFix;
            ++ j;

#if WATCH_DENORMALS
            Denormal.CheckDenormal( "FIR", leftOutput );
            Denormal.CheckDenormal( "FIR", rightOutput );
#endif
         }

         _denormalFix = - _denormalFix;

         // Shift the buffer left, or...shift the coefficients to
         // the right. Same thing really. Fake circular buffers. :/

         if (_offset <= 0)
         {
            _offset = delayLineLeft.Length - 1;
            _buffOffset = 0;
         }
         else
         {
            -- _offset;
            ++ _buffOffset;
         }

         leftInput = leftOutput;
         rightInput = rightOutput;
      }

      double [] _coef;
      int _offset = 0;          // current offset into _coef
      int _buffOffset = 0;      // current offset into _buff

      double _denormalFix = Denormal.denormalFixValue;
   }
}
