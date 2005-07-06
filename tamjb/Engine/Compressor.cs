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

         // Initialize compression times
         compressAttack = 0.010; // seconds
      }

      ///
      /// Implements IAudioProcessor. Process this buffer.
      ///
      public void Process( ref double left,
                           ref double right )
      {
         // Approximate the average power of each channel, then
         // figure the average of the two channel's average power.

         _decayingAveragePowerLeft = 
            ((_decayingAveragePowerLeft * _rmsDecayOld) +
             (Math.Abs(left) * _rmsDecayNew));

         if (_decayingAveragePowerLeft < 1.0e-25) // avoid denormals
            _decayingAveragePowerLeft = 0.0;

         _decayingAveragePowerRight = 
            ((_decayingAveragePowerRight * _rmsDecayOld) +
             (Math.Abs(right) * _rmsDecayNew));
         
         if (_decayingAveragePowerRight < 1.0e-25) // avoid denormals
            _decayingAveragePowerRight = 0.0;

         double avgPower = (_decayingAveragePowerLeft 
                            + _decayingAveragePowerRight) / 2;

         // Don't correct gain if we're below the threshold
         double newCorrection;
         if (avgPower < _gateLevel)
            newCorrection = _targetPowerLevel / _gateLevel;
         else
            newCorrection = _targetPowerLevel / avgPower;

         // For inf:1 compression, use "offset", otherwise
         // use this ratio to get other ratios:
         newCorrection = (newCorrection * _compressRatio) 
            + (1.0 - _compressRatio);

         // On release, use exponential decay in addition to the 
         // lowpass. 
         if (newCorrection > _correction) // Gain is increasing?
         {
            _correction = 
               ((_correction * _decayRatioOld) +
                (newCorrection * _decayRatioNew));
         }
         else
         {
            _correction = newCorrection;
         }

         // Avoid clicks. Use lowpass filter (attack ratio) on both
         // attack and release. Do attack filtering after the release
         // so that the release rate doesn't mess up the attack rate
         // (because I'm using a lowpass, if it thinks gain was rapidly
         // increasing, this will decrease the attack rate, which is bad).
         newCorrection = _correctionFilter.Process( newCorrection );


#if WATCH_DENORMALS
         // Correction should never remotely approach 0
         Denormal.CheckDenormal( "Comp correction[2]", newCorrection );
#endif

         // Store samples to circular buffer, and save here.
         // Note that the lookahead is hardcoded and you're stuck
         // with it. :) 
         // Use the returned value from the circular buffer as the
         // current value. This introduces a delay, naturally.

         // Write new values to the samples: left

         left = (left * _correction) + _denormalFix;
         left = _leftFifo.Push( left );

         // Now the right!

         right = (right * _correction) + _denormalFix;
         right = _rightFifo.Push( right );

         // Now your left!
         
         // No, that's your right.
         
         // (Aqua Teen Rules!)

         _denormalFix = - _denormalFix;

#if WATCH_DENORMALS
         Denormal.CheckDenormal( "Comp out left", left );
         Denormal.CheckDenormal( "Comp out right", right );
#endif

      }

      ///
      /// Attack time in seconds
      ///
      public double compressAttack
      {
         get
         {
            return _compressAttack;
         }
         set
         {
            if (value < Denormal.denormalFixValue)
            {
               throw new ArgumentException( "Attack too small",
                                            "compressAttack" );
            }

            _correctionFilter.Initialize( 1.0 / _compressAttack,
                                          44100.0 );

            _compressAttack = value;
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
      /// RMS power around 4k seems common enough.
      ///
      double _targetPowerLevel = 4000.0;

      ///
      /// Level below which we stop compressing and start
      /// expanding (if possible)
      ///
      double _gateLevel = 500.0;

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
      double _compressRatio = 1.0;

      //
      // The average power should have a decay at least long enough to allow
      // detecting low frequencies (20 Hz). Attack/decay the same to calculate
      // a time-decaying RMS power.
      //
      double _rmsDecayNew = 0.002; // Note: should depend on sample rate!
      double _rmsDecayOld = 0.998;

      double _decayRatioNew = 0.0002;
      double _decayRatioOld = 0.9998;

      //
      // gain correction as used in the current & previous sample
      //
      double _correction = 1.0; 

      double _compressAttack = 0.3;
      FirstOrderLowpassFilter _correctionFilter = 
      new FirstOrderLowpassFilter();

      //
      // This is the average rms power of the current track decaying
      // exponentially over time. Normalized to 16 bits.
      //
      // Initially maxed out to avoid clipping
      //
      double _decayingAveragePowerLeft = (float)32767.0;
      double _decayingAveragePowerRight = (float)32767.0;

      ///
      /// The short delay used to allow the compressor attack to precede
      /// the actual event that caused it, allowing negative delay.
      ///
      // Note: the fifo is initially filled with 0, which is silence
      //       with signed 16-bit samples. :)
      SampleFifo _leftFifo = new SampleFifo( MAX_PREDELAY, 0 );
      SampleFifo _rightFifo = new SampleFifo( MAX_PREDELAY, 0 );

      double _denormalFix = Denormal.denormalFixValue;
   }
}
