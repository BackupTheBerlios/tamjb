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

   // using byteheaven.tamjb.Interfaces;

   ///
   /// A simple average-power based compressor using exponential
   /// decay based attack/release (so it tends to click if it's
   /// too aggressive). 
   ///
   public class Compressor
      : IAudioProcessor
   {
      ///
      /// Maximum value for the compressPredelay value. (samples)
      ///
      public readonly static uint MAX_PREDELAY = 88;

      public Compressor()
      {
         // Initially select 1/2 millisecond for the delay as a default.
         _leftFifo.delay = 22;
         _rightFifo.delay = 22;
      }

      ///
      /// Implements IAudioProcessor. Process this buffer.
      ///
      public void Process( ref double left,
                           ref double right )
      {
         // Approximate the average power of both channels by summing
         // them (yes, phase differences are a problem but whatever)
         // And exponentially decay or release the level:
               
         // Fast attack, slow decay
         double magnitude;
         magnitude = Math.Abs(left);
         if (magnitude > _decayingAveragePower)
         {
            _decayingAveragePower = 
               ((_decayingAveragePower * _attackRatioOld) +
                (magnitude * _attackRatioNew));
         }
         else
         {
            _decayingAveragePower = 
               ((_decayingAveragePower * _decayRatioOld) +
                (magnitude * _decayRatioNew));
         }

         magnitude = Math.Abs(right);
         if (magnitude > _decayingAveragePower)
         {
            _decayingAveragePower = 
               ((_decayingAveragePower * _attackRatioOld) +
                (magnitude * _attackRatioNew));
         }
         else
         {
            _decayingAveragePower = 
               ((_decayingAveragePower * _decayRatioOld) +
                (magnitude * _decayRatioNew));
         }

         // How far off from the target power are we?
         double correction;
         if (_decayingAveragePower < _gateLevel)
            correction = _targetPowerLevel / _gateLevel;
         else
            correction = _targetPowerLevel / _decayingAveragePower;

         // For inf:1 compression, use "offset", otherwise
         // use this ratio to get other ratios:
         correction = (correction * _compressRatio) 
            + (1.0 - _compressRatio);

         // Store samples to circular buffer, and save here.
         // Note that the lookahead is hardcoded and you're stuck
         // with it. :) 
         // Use the returned value from the circular buffer as the
         // current value. This introduces a delay, naturally.

         // Write new values to the samples: left

         left = left * correction;
         left = _leftFifo.Push( left );

         // Now the right!

         right = right * correction;
         right = _rightFifo.Push( right );

         // Now your left!
         
         // No, that's your right.
         
         // (Aqua Teen Rules!)
      }

      //
      // Attack ratio per-sample. Hmmm.
      //
      public double compressAttack
      {
         get
         {
            return _attackRatioNew;
         }
         set
         {
            _attackRatioNew = value;
            if (_attackRatioNew >= 1.0)
               _attackRatioNew = 1.0;
            
            if (_attackRatioNew <= 0.0)
               _attackRatioNew = 0.0;
            
            _attackRatioOld = 1.0 - _attackRatioNew;
         }
      }

      public double compressDecay
      {
         get
         {
            return _decayRatioNew;
         }
         set
         {
            _decayRatioNew = value;
            if (_decayRatioNew >= 1.0)
               _decayRatioNew = 1.0;
            
            if (_decayRatioNew <= 0.0)
               _decayRatioNew = 0.0;
            
            _decayRatioOld = 1.0 - _decayRatioNew;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public int compressThreshold
      {
         get
         {
            return (int)_targetPowerLevel;
         }
         set
         {
            _targetPowerLevel = (double)value;
            
            if (_targetPowerLevel >= 32767.0)
               _targetPowerLevel = 32767.0;
            
            if (_targetPowerLevel < 1.0) // Uh, you WANT complete silence?
               _targetPowerLevel = 1.0;
         }
      }

      ///
      /// Input level gate threshold (0-32767). Probably should be
      /// less than the compress threshold. :)
      ///
      public int gateThreshold
      {
         get
         {
            return (int)_gateLevel;
         }
         set
         {
            _gateLevel = (double)value;
            
            if (_gateLevel >= 32767.0)
               _gateLevel = 32767.0;
            
            if (_gateLevel < 0.0)
               _gateLevel = 0.0;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public double compressRatio
      {
         get
         {
            return _compressRatio;
         }
         set
         {
            _compressRatio = value;
               
            if (_compressRatio > 1.0)
               _compressRatio = 1.0;
               
            if (_compressRatio < 0.0) // Uh, you WANT complete silence?
               _compressRatio = 0.0;
         }
      }

      ///
      /// This controls how much in advance of the beat the attack
      /// may occur.
      ///
      public uint compressPredelay
      {
         get
         {
            return _leftFifo.delay;
         }
         set
         {
            _leftFifo.delay = value;
            _rightFifo.delay = value;
         }
      }


      ///
      /// Target power level for compression/expansion.
      /// 
      /// Hot-mastered albums have an average power level with
      /// like -3dB from the absolute max level. Oh well, might 
      /// as well match that.
      ///
      double _targetPowerLevel = 16000.0;

      ///
      /// Level below which we stop compressing and start
      /// expanding (if possible)
      ///
      double _gateLevel = 1000.0;

      /// 
      /// Compression ratio where for n:1 compression, 
      /// RATIO = (1 - 1/n).  or something.
      ///
      /// 1.0 = infinity:1
      /// 0.875 = 8:1
      /// 0.833 = 6:1
      /// 0.75 = 4:1
      /// 0.5 = 2:1
      /// 0.0 = no compression 
      ///
      double _compressRatio = 0.833;

      //
      // Attack time really should be more than a 10 milliseconds to 
      // avoid distortion on kick drums, unless the release time is 
      // really long and you want to use it as a limiter, etc.
      //
      double _attackRatioNew = 0.002;
      double _attackRatioOld = 0.998;

      double _decayRatioNew = 0.00000035;
      double _decayRatioOld = 0.99999965;

      //
      // This is the average rms power of the current track decaying
      // exponentially over time. Normalized to 16 bits.
      //
      // Initially maxed out to avoid clipping
      //
      double _decayingAveragePower = (float)32767.0;

      ///
      /// The short delay used to allow the compressor attack to precede
      /// the actual event that caused it, allowing negative delay.
      ///
      // Note: the fifo is initially filled with 0, which is silence
      //       with signed 16-bit samples. :)
      SampleFifo _leftFifo = new SampleFifo( MAX_PREDELAY, 0 );
      SampleFifo _rightFifo = new SampleFifo( MAX_PREDELAY, 0 );
   }
}
