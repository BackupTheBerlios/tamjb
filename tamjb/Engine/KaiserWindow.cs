/// \file
/// $Id$
/// Copyright (C) 2005 Tom Surace. All rights reserved.

namespace byteheaven.tamjb.Engine
{
   using System;
   using System.Diagnostics;

   ///
   /// Computes a kaiser window for given parameters. It's a DSP thing.
   ///
   class KaiserWindow
   {
#if INCLUDE_MAIN      
      static void _Usage()
      {
         Console.Error.WriteLine( "Required parameters: cutoff, width" );
         Console.Error.WriteLine( "  -p         Passband freq" );
         Console.Error.WriteLine( "  --passband" );
         Console.Error.WriteLine( "  -s         Stopband freq" );
         Console.Error.WriteLine( "  --stopband" );
         Console.Error.WriteLine( "  -A         A (60)" );
         Console.Error.WriteLine( "  -m         Max M (1500)" );
         Console.Error.WriteLine( "  --maxM" );
         Console.Error.WriteLine( "  -n         Nyquist frequency (44100)" );
         Console.Error.WriteLine( "  --nyquist" );
      }

      static int Main( string [] argv )
      {
         double nyquist = 44100.0;
         double passband = 0.0;
         double stopband = 0.0;
         int  maxM = 1500;
         double A = 60; 

         int i = 0;
         while ((i + 1) < argv.Length)
         {
            string command = argv[i];
            string val = argv[i + 1];
            switch (command)
            {
            case "-p":
            case "--passband":
               passband = Convert.ToDouble( val );
               break;
               
            case "-s":
            case "--stopband":     // in octaves?
               stopband = Convert.ToDouble( val );
               break;

            case "-A":
               A = Convert.ToDouble( val );
               break;

            case "-m":
            case "--maxM":
               maxM = Convert.ToInt32( val );
               break;

            case "-n":
            case "--nyquist":
               nyquist = Convert.ToDouble( val );
               break;

            default:
               _Usage();
               return 1;
            }

            i += 2;
         }

         if (0.0 == passband 
             || 0.0 == stopband
             || 0 == maxM )
         {
            _Usage();
            return 1;
         }

         // OK, convert frequency to fraction of nyquist / 2 (aka 2*pi)
         passband = passband / (nyquist / 2.0);
         stopband = stopband / (nyquist / 2.0);

         Console.Error.WriteLine( "# passband: {0} * pi", passband );
         Console.Error.WriteLine( "# stopband: {0} * pi", stopband );
         Console.Error.WriteLine( "# A: {0}", A );

         double beta;
         double [] points = FromParameters( passband,
                                            stopband,
                                            A,
                                            maxM,
                                            out beta );

         // Prints output suitable for fiview:

         Console.Error.WriteLine( "# B:{0}", beta );
         Console.Error.WriteLine( "# M:{0}", points.Length );

         Console.Write( "x " );
         foreach (double point in points)
         {
            Console.Write( "{0} ", point );
         }
         return 0;
      }

#endif // INCLUDE_MAIN

      ///
      /// Returns a Kaiser/Bessel window for the given parameters, as an 
      /// array of doubles. Which does, indeed, have some redundancy.
      ///
      /// \param passBand Width (from 0-1) of the passband
      ///
      /// \param stopBand is the start frequency for the passband. Must
      ///   be larger than the passband. :)
      ///
      /// \param error is the maximum cutoff error. This function takes
      ///   the parameter in decibels, sort of. (-20 log sigma, (aka "A"))
      ///
      /// \maxM is the maximum window width (M) that is acceptable. If the
      ///   filter requires a higher value of M, an exception is thrown.
      ///   If you want a sharp cutoff and little ripple, this can be large.
      ///  
      static public double [] FromParameters( double passBand,
                                              double stopBand,
                                              double errorDb,
                                              int maxM,
                                              out double beta )
      {
         if (passBand >= stopBand)
         {
            throw new ArgumentException( "passband and stopband overlap",
                                         "passband" );
         }

         if (stopBand > 1.0)
         {
            throw new ArgumentException( "stopband frequency larger than nyquist",
                                         "passBand" );
         }

         if (passBand < 0.0)
         {
            throw new ArgumentException( "Passband frequency is negative",
                                         "passBand" );
         }

         double width = stopBand - passBand;

         // Cutoff is halfway between the frequencies
         double cutoff = (passBand + stopBand) / 2;

         // It's actually freq * PI, I just allow the parameters
         // to be otherwise. Hmmm.

         // Console.Error.WriteLine( "cut:{0} width:{1}", cutoff, width );

         stopBand *= Math.PI;
         passBand *= Math.PI;
         cutoff *= Math.PI;
         width *= Math.PI;

         beta = CalcBeta( errorDb );
         int M = CalcM( errorDb, width );

         if (M > maxM)
         {
            throw new ApplicationException
               ( "M exceeds maximum specification: "
                 + M.ToString() );
         } 

         return CalcImpulse( M, beta, cutoff );
      }

      static public double CalcBeta( double errorDb )
      {
         // Kaiser did a lot of trial and error to bring us this
         // tasty goodness.

         if (errorDb > 50) // aka "A"
         {
            return 0.1102 * (errorDb - 8.7);
         }
         else if (errorDb >= 21) // 21 <= errorDb <= 50
         {
            return (0.5842 * Math.Pow( (errorDb - 21), 0.4 ))
               + (0.07886 * (errorDb - 21));
         }
         else
         {
            Debug.Assert( errorDb < 21 );

            return 0.0;
         }
      }

      static public int CalcM( double errorDb, double transitionWidth )
      {
         // I round to the nearest even number, so that the delay
         // of the filter is an integer. (And thus we have a Type I
         // linear phase filter.)
         int newM = (int)((errorDb - 8) / (2.285 * transitionWidth));

         return newM - (newM % 2); // round off to nearest odd number
      }

      static public double [] CalcImpulse( int M, double beta, double cutoff )
      {
         double [] impulse = new double[ M ];
         double halfM = ((double)M) / 2.0;

         // Calculate the window (Oppenheimer & Schafer Sec 7.4 1989 ed)

         for (int n = 0; n < M; n++)
         {
            // Whoa nelly. OK, so if our window incorporates this factor,
            // it will be linear phase:
            double phaseFactor;
            double nMinusHalfM = n - halfM;
            if (nMinusHalfM == 0) // Avoid overflow
            {
               // Evaluates to, er, sin(0), because it was approaching
               // 0 anyway. So, this approaches:
               phaseFactor = cutoff / Math.PI;
            }
            else
            {
               phaseFactor =
                  Math.Sin(cutoff * nMinusHalfM) 
                  / (Math.PI * nMinusHalfM)
                  ;
            }

            // Kaiser worked this out for us, which gives predictable
            // results if you use his equations for M and beta. Probably.
            double kaiserWindow =
               (Bessel.IZero( beta *
                              Math.Sqrt(1 - Math.Pow( (n-halfM)/halfM, 2 )) )
                / Bessel.IZero(beta))
               ;

            double point = phaseFactor * kaiserWindow;

            if (Double.IsNaN(point))
            {
               throw new ApplicationException( 
                  String.Format( "Bad Kaiser Window Point pf:{0} kw:{1} n:{2}", 
                                 phaseFactor,
                                 kaiserWindow,
                                 n ) );
            }

            impulse[n] = point;
         }

         return impulse;
      }
   }
}
