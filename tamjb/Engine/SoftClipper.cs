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
   using System.Configuration;
   using System.IO;

   ///
   /// Soft clipping based on antilog (1/x)
   /// based nonlinearity. ("Asympote!" "What did you just call me?")
   ///
   public class SoftClipper
      : IAudioProcessor
   {

      public void Process( ref double left, ref double right )
      {
         left = _SoftClip( left );
         right = _SoftClip( right );
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public int clipThreshold
      {
         get
         {
            return (int)_clipThreshold;
         }
         set
         {
            _clipThreshold = (double)value;
            
            if (_clipThreshold >= 32766.0)
               _clipThreshold = 32766.0;
               
            if (_clipThreshold < 1.0) // Uh, you WANT complete silence?
               _clipThreshold = 1.0;
               
            _clipLeftover = 32767.0 - _clipThreshold;
         }
      }

      ///
      /// This function clips based on the current settings. Note
      /// that sevral audio streams can share this, because the 
      /// SoftClip maintains no state between calls. (aside from the 
      /// threshold).
      ///
      double _SoftClip( double original )
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
      // be 32767 - _clipThreshold.
      double _clipThreshold = 16383.5;
      double _clipLeftover =  16383.5;

   }
}
