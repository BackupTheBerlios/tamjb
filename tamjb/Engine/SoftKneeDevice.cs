/// \file
/// $Id$
///

// Copyright (C) 2004-2006 Tom Surace.
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

   ///
   /// A base class for devices that perform soft-knee processing (such
   /// as clipping or limiting) based on antilog (1/x)
   /// based nonlinearity. ("Asympote!" "What did you just call me?")
   ///
   public class SoftKneeDevice
   {
      ///
      /// Note that the default range is 0-32767, with a threshold
      /// of 16383.5
      ///
      public SoftKneeDevice()
      {
         clipThreshold = 16383.5;
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      /// The upper limit is always twice the clip threshold.
      ///
      public double clipThreshold
      {
         get
         {
            return _clipThreshold;
         }
         set
         {
            // For a smooth crossover between the linear and
            // clipping parts, the slope should be 1. So, correct crossover
            // values can be found using this equation: (unless some idiot
            // changes the clipping value below without doing this again)
            // 
            // 1 = (_clipThreshold * _clipLeftover) / _clipThreshold^2
            //
            // Solving the equation, and substituting _clipLeftover = 
            // Max - _clipThreshold, we see that the only possible 
            // crossover point is where
            //
            //   _clipThreshold == _clipLeftover

            _clipThreshold = value;
            _clipLeftover = _clipThreshold;
         }
      }

      ///
      /// This function clips based on the current settings. Note
      /// that sevral audio streams can share this, because the 
      /// SoftClip maintains no state between calls. (aside from the 
      /// threshold).
      ///
      /// This function can also be used to calculate the output peak
      /// level for a given input level (and therefore the gain for that
      /// input level).
      ///
      public double SoftClip( double original )
      {
         if (original > _clipThreshold) // Soft-clip 
         {
            // Unsophisticated asympotic clipping algorithm
            // I came up with in the living room in about 15 minutes.
            return (_clipThreshold +
                    (_clipLeftover * (1 - (_clipThreshold / original)))); 
         }
         else if (original < (- _clipThreshold))
         {
            // Unsophisticated asympotic clipping algorithm
            // I came up with in the living room in about 15 minutes.
            return ((-_clipThreshold) -
                    (_clipLeftover * (1 + (_clipThreshold / original)))); 
         }

         return original;
      }

      // Sample value for start of soft clipping. Leftover must
      // be the same as threshold for the settings to sound good,
      // or at least whatever value of x such that dy/dt == 1, which
      // I haven't worked out yet since I can't remember how to do
      // calculus until I get home to my textbooks.
      double _clipThreshold;
      double _clipLeftover;

   }
}
