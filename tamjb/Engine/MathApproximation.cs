/// \file
/// $Id$
///

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
   
   public class MathApproximation
   {
//       public static void Main()
//       {
//          double [] values = { 1.0, 2.28, 1.69, 2.929, 1.13, 
//                               0.98, 0.21, 5.9 };
         
//          foreach (double val in values)
//          {
//             double approx = AntiLog10Approx( val );
//             double actual = Math.Pow( 10.0, val );
            
//             Console.WriteLine( "val:{0} approx:{1} ({2})",
//                                val,
//                                approx,
//                                actual );
//          }
//       }


      /// Helper table for Antilog base 10
      ///
      static double [] antilog_1_0 = new double[ 6 ] {
         1,
         10,
         100,
         1000,
         10000,
         100000
      };

      static double [] antilog_0_1 = new double[ 10 ] {
         1,
         1.258925412,           // 0.1
         1.584893192,
         1.995262315,
         2.511886432,
         3.162277660,           // .5
         3.981071706,
         5.011872336,
         6.309573445,
         7.943282347
      };

      static double [] antilog_0_01 = new double[ 11 ] {
         1,
         1.023292992,           // 0.01
         1.047128548,
         1.071519305,
         1.096478196,
         1.122018454,           // .05
         1.148153621,
         1.174897555,
         1.202264435,
         1.230268771,
         1.258925412            // 0.10 (because we round the last digit up)
      };

      ///
      /// Antilog (base 10) approximation
      ///
      /// \warn Input must be >= 0.0 and < 6.0!
      ///
      /// The antilog approximation only has to handle input values
      /// representing average power of a 16-bit value, which is to say
      /// For 1-32767. We can ignore values below 1 because it's an 
      /// integer.
      ///
      /// Therefore, the output range is 0 < x < 4.516. Implemented here
      /// with a range of 0 to 5.99, and a couple of significant digits
      /// throughout most of the rang.
      ///
      static public double AntiLog10( double input )
      {
//          Debug.Assert( input < 6.0, "Log10 approximation input out of range" );
//          Debug.Assert( input >= 0.0, "negative inputs not supported" );

         double output = antilog_1_0[ (int)input ];

         // Alternative method: may be faster with these "safe" arrays:
//          while (input > 1.0)
//          {
//             output *= 10.0;     // 10^1
//             input -= 1.0;
//          }

         // The index into the one-tenths-place array is the remainder * 10
         // truncated to an integer. 
         input = (input - (int)input) * 10.0;
         output *= antilog_0_1[ (int)input ];

         // round this last one off. (Note that the array then is
         // 11 entries long). 
         //
         // TODO: instead of rounding, interpolate using the remainder 
         // to avoid zippering effects. We can probably skip this last
         // array lookup when this is implemented.
         
         input = Math.Round( (input - (int)input) * 10.0 );
         output *= antilog_0_01[ (int)input ];

         return output;
      }

      static double logBase10of2 = 0.301;

      ///
      /// A very poor approximation of Log (base 10). Has about 1 
      /// significant digit of usefulness. And...it's much slower than
      /// using the builtin Math.Log10 function. The magic of boxing,
      /// I guess.
      ///
      /// This seems to be quite a bit slower than Math.Log10, but less
      /// likely to result in occasional very-long computation times.
      ///
      public static double Log10Poor( double input )
      {
         // Very quick and dirty log2 approximation: just use the 
         // exponent (which is a 11 bit integer with offset, so
         // more or less a signed integer. Basically.)

         // log10(x) ~= log10(2) * log2(x) = 0.301 * log2(x),
         // gets better for large x.
         // Approximate log2 by grabbing the double's exponent.

         // Without the mantissa, you get 1 sig fig. :)
         // long mantissa = bits & 0xfffffffffffff; // 13 f's
         // But denormalized numbers are freaky so ignore it unless
         // there's a good reason.

         // Turn the 11 bits of the exponent into a number by masking
         // and subtracting 1023. Then multiply by log10(2) and we're
         // sort of nearly there.
         return logBase10of2
            * (((int)(BitConverter.DoubleToInt64Bits( input ) >> 52)
                & 0x7ff)
               - 1023);
      }


// Clay S. Turner  	  Nov 19 2001, 11:01 am     show options
// Newsgroups: comp.dsp
// From: "Clay S. Turner" <phys...@bellsouth.net> - Find messages by this author
// Date: Mon, 19 Nov 2001 10:59:50 -0500
// Local: Mon,Nov 19 2001 10:59 am
// Subject: Re: Log10 sur DSP ?
// Reply to Author | Forward | Print | Individual Message | Show original | Report Abuse

// Hello Olivier,
//         For a real rough approx., just find the bit position of the left most
// 1's bit, and mult by 0.301.
//       public double _LogApprox_1( double input )
//       {
         
//       }

// One mod to the algo (one extra mult)

// y=A*LMO(B*x)  LMO is left most ones position

// A=log(2)  apprx 0.301029995
// B=sqrt(2) apprx 1.414213562 

//       public double _LogApprox_2( double input )
//       {
         
//       }

// Olli Niemitalo  	  Nov 19 2001, 10:45 am     show options
// Newsgroups: comp.dsp
// From: Olli Niemitalo <oniem...@mail.student.oulu.fi> - Find messages by this author
// Date: Mon, 19 Nov 2001 17:30:58 +0200
// Local: Mon,Nov 19 2001 10:30 am
// Subject: Re: Log10 sur DSP ?
// Reply to Author | Forward | Print | Individual Message | Show original | Report Abuse

// On Mon, 19 Nov 2001, Olivier Omedes wrote:
// > Does somebody know how to implement a log10 operand with a 32 bits
// > dynamic on a dsp ? I do not need high precision, only a quick
// > apporximation.

// First you can calculate log2 of the input. Then divide (or multiply) log2
// by an appropriate constant to convert to base 10.

// Log2 can easily be approximated with linear segments. If you had a
// floating point input, you'd just take the exponent and that would be your
// integer part of the result. The fractional part would then be the
// mantissa. For a fixed point (unsigned) input, you'd count the leading
// zeros and use the bits after the first one-bit as the fractional part.




// robert bristow-johnson  	  Mar 29 1999, 1:00 am     show options
// Newsgroups: comp.dsp
// From: robert bristow-johnson <pbj...@viconet.com> - Find messages by this author
// Date: 1999/03/29
// Subject: Re: Looking for good implementation of log() and sqrt()
// Reply to Author | Forward | Print | View Thread | Show original | Report Abuse
//
//         2^x ~=
//                                  1.0
//                         +        0.6930321187 * x
//                         +        0.2413797743 * x^2
//                         +        0.0520323499 * x^3
//                         +        0.0135557571 * x^4
//
// 0 <= x <= 1     |error|/(2^x) < 3.340e-6  (exact when x=0 and x=1)

// 10^x = 2^(log2(10)*x) ?????

// If you write x = q + r, then 10^x = 10^q * 10^r.  If you use as much
// memory as you can afford to tabulate 10^q, then you can reduce the
// size of r, so that you need only a few terms of the series. 

// Say, have a table for each octave, then?

// Tables:
// 10^10.2 = 10^10 * 10^0.2 = 10^10 * 10^0.1 * 10^0.1

//
// We can probably ignore the case where average power < 1.0, since that's
// less than 1 sample.

   } // class
} // namespace
