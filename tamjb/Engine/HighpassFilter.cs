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
   /// This implementation is based on Bram's 
   /// first-order highpass from www.musicdsp.org, which (like the lowpass)
   /// uses exponential decay, but subtracts it to form a highpass. 
   /// The cool part of this is the code to calculate
   /// the decay coefficient, but the uncool part is that it always seems
   /// to give me a coefficient of 1, which means no output occurs. :)
   ///
   /// \code 
   /// 
   /// recursion: tmp = (p-1)*in - p*tmp with output = tmp
   /// coefficient: p = (2+cos(x)) - sqrt((2+cos(x))^2 - 1) 
   ///    with x = 2*pi*cutoff/samplerate
   ///
   /// note: in recursion, tmp starts as the prev output.
   /// \endcode
   ///
   public class HighpassFilter : IAudioProcessor
   {
      ///
      /// \note Assumes sample rate of 44100
      ///
      public HighpassFilter()
      {
         _leftFilter = new MonoHighpassFilter();
         _rightFilter = new MonoHighpassFilter();

         // Use public "set" function to initialize everything:
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

      ///
      /// Implmenets IAudioProcessor.Process.
      ///
      public void Process( ref double left, ref double right )
      {
         left = _leftFilter.Process( left );
         right = _rightFilter.Process( right );
      }

      double _cutoff = 100.0;    // Not used in calculations

      IMonoFilter _leftFilter;
      IMonoFilter _rightFilter;
   }

}
