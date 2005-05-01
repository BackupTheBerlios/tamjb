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
   using System.Configuration;
   using System.IO;

   using byteheaven.tamjb.Interfaces;

   ///
   /// This implementation is based mostly on Bram's 
   /// first-order lowpass from www.musicdsp.org, which just uses
   /// exponential decay. The cool part of this is the code to calculate
   /// the decay coefficient, which works nicely for lower frequencies.
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
   public class LowpassFilter : IAudioProcessor
   {
      ///
      /// \note Assumes sample rate of 44100
      ///
      public LowpassFilter()
      {
         _leftFilter = new MonoLowpass();
         _rightFilter = new MonoLowpass();

         cutoff = 150.0;        // hz
      }

      public double cutoff
      {
         get
         {
            return _cutoff;
         }
         set
         {
            _cutoff = value;

            _leftFilter.Initialize( cutoff, 44100.0 );
            _rightFilter.Initialize( cutoff, 44100.0 );
         }
      }

      void _Initiailize( double cutoff )
      {
         _leftFilter.Initialize( cutoff, 44100.0 );
         _rightFilter.Initialize( cutoff, 44100.0 );
      }

      ///
      /// Implmenets IAudioProcessor.Process.
      ///
      public void Process( ref double left, ref double right )
      {
         left = _leftFilter.Process( left );
         right = _rightFilter.Process( right );
      }

      double _cutoff = 100.0;    // Not used in calculations

      MonoLowpass _leftFilter;
      MonoLowpass _rightFilter;

      class MonoLowpass
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

}
