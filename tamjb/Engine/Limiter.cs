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
   /// A VERY simple limiter. That sounds surprisingly decent. Heh.
   ///
   public class Limiter : IAudioProcessor
   {
      public double limit 
      {
         set
         {
            _upLimit = value;
            _lowLimit = -value;
         }

         get
         {
            return _upLimit;
         }
      }

      public bool isLimiting 
      {
         get
         {
            return (_gain < 0.999);
         }
      }

      void _FixGainPos( ref double level )
      {
         double newGain = _upLimit / level;
         if (newGain < _gain)
            _gain = newGain;
      }

      void _FixGainNeg( ref double level )
      {
         double newGain = _lowLimit / level;
         if (newGain < _gain)
            _gain = newGain;
      }

      public void Process( ref double left, ref double right )
      {
         // Work your way back towards a gain of 1.
         _gain = (_gain * _gainKeep) + _gainChange;

         if (left > _upLimit)
            _FixGainPos( ref left );
         else if (left < _lowLimit)
            _FixGainNeg( ref left );

         if (right > _upLimit)
            _FixGainPos( ref right );
         else if (right < _lowLimit)
            _FixGainNeg( ref right );

         left = left * _gain;
         right = right * _gain;
      }


      double _upLimit = 32767.0;
      double _lowLimit = -32767.0;

      // Current gain.
      double _gain = 1.0;

      // This should give a very poor approximation of a 10-20 ms release
      // time. Did I mention that it's very poor?
      double _gainKeep = 0.99999;
      double _gainChange = 0.00001;

   }
}
