/// \file
/// $Id$
///
/// Copyright (C) 2005 Tom Surace. All rights reserved.

namespace byteheaven.tamjb.Engine
{
   using System;
   using System.Diagnostics;

   ///
   /// Bessel functions. Well, a subset of them. Well, one. 
   ///
   class Bessel
   {
      ///
      /// Modified zero-order bessel function with fixed error and
      /// random run time.
      ///
      static public double IZero( double val )
      {

         // I0 = sum 0-k ((0.5*x)^k/k!)^2

         // Note how this skips the first loop (i=0), because we know
         // the output value regardless.
         double output = 1.0; 

         double factorial = 1.0;
         int i = 1;
         while (true)           // loop forever
         {
            // Keep computing i! (factorial of i) in the loop.
            factorial *= i;

            // That outer "squared" bit is pulled into the loop for
            // no apparent reason by several different authors I've
            // seen, so I'm doing it too. 
            // As far as I can tell, this actually results in
            // an additional multiplication operation and saves one
            // register or variable.

            double change = 
               Math.Pow( val/2.0 , i * 2 ) / (factorial * factorial);

            // change *= change;   // squared?

            output += change;

            // Console.WriteLine( "{0}, {1}", output, change );

            // Depending on how much error you care about, drop out sooner
            // or later. Kaiser dropped out (not too good at fortran, but...)
            // when the change was below a certain fraction of the value. 
            if (change < (output * 0.2e-8))
               break;

            if (i > 49)
            {
               Console.WriteLine( "bessel function--bailing out:"
                                  + output );
               break;
            }

            ++ i;
         }

	return output;
      }
#if IGNORE

_ftype_t besselizero(_ftype_t x)
{ 
  _ftype_t temp;
  _ftype_t sum   = 1.0;
  _ftype_t u     = 1.0;
  _ftype_t halfx = x/2.0;
  int      n     = 1;

  do {
    temp = halfx/(_ftype_t)n;
    u *=temp * temp;
    sum += u;
    n++;
  } while (u >= BIZ_EPSILON * sum);
  return(sum);
}

#endif


   }
}
