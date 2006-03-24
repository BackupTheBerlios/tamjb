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
      public readonly static uint MAX_PREDELAY = (uint)(44.1 * 200.0);

      ///
      /// Set to true to have the compressor gradually learn the
      /// average level of all songs, thus automatically adjusting
      /// all incoming audio gradually to the average level learned
      /// from all audio to date.
      ///
      public bool doAutomaticLeveling
      {
         get
         {
            return _learnTargetPower;
         }
         set
         {
            _learnTargetPower = value;
         }
      }

      public Compressor()
      {
         // Initially select 1/2 millisecond for the delay as a default.
         _leftFifo.delay = 22;
         _rightFifo.delay = 22;

         // Initialize compression times
         compressAttack = 0.010; // seconds
         compressDecay = 5.0;   // seconds

         _learnTargetPower = false;
      }

      ///
      /// Implements IAudioProcessor. Process this buffer.
      ///
      public void Process( ref double left,
                           ref double right )
      {
         // Approximate the average power of each channel, then
         // figure the average of the two channel's average power.

         double avgPower = (Math.Abs(left) + Math.Abs(right)) / 2;

         // Gradually adjust target power?
         if (_learnTargetPower)
         {
            // How fast to adjust? Slowly! We want the average of many
            // songs, not just one. A 3.5 minute song at 44100 has
            // 9.2 million samples in it, and we want to adjust, say, 50% of
            // the scale over, say, 10 songs. Hmm.
            //
            // Note that I increase the target power because the 
            // compressor mostly uses the peak level (unless I do some
            // radical changes to how it works).
            if (avgPower > (_targetPowerLevel / 2.0))
               _targetPowerLevel += 0.000001;
            else
               _targetPowerLevel -= 0.000001;
         }


         // Limit the rate of change of the percieved average power,
         // and let the instantaneous correction take care of itself

         // Soft-knee is pretty much like this:
//         y=yo*(x/xo)^r      r=ratio, xo=threshold, so
//         20*log10(y/yo)=r*20*log10(x/xo)   - 1 dB in -> r dB out 

         // log-linear attack/release

         // This is a bad approximatino AND it's very slow. :(
         // double logAvgPowerNew = MathApproximation.Log10Poor( avgPower );
         double logAvgPowerNew = Math.Log10( avgPower );

         if (logAvgPowerNew > _avgPowerLog)
         {
            _avgPowerLog += _compressAttackRate;
            if (_avgPowerLog > logAvgPowerNew)
               _avgPowerLog = logAvgPowerNew;
         }
         else
         {
            _avgPowerLog -= _compressReleaseRate;
            if (_avgPowerLog < logAvgPowerNew)
               _avgPowerLog = logAvgPowerNew; // don't pass the new value
         }

         // Convert back into a 16-bit "number" for the correction math
         avgPower = MathApproximation.AntiLog10( _avgPowerLog );

         // Don't correct gain if we're below the threshold
         if (avgPower < _gateLevel)
            _correction = _targetPowerLevel / _gateLevel;
         else
            _correction = _targetPowerLevel / avgPower;

         // For inf:1 compression, use "offset", otherwise
         // use this ratio to get other ratios:
         _correction = (_correction * _compressRatio) 
            + (1.0 - _compressRatio);

#if WATCH_DENORMALS
         // Correction should never remotely approach 0
         Denormal.CheckDenormal( "Comp correction[2]", _correction );
#endif

         // Store samples to circular buffer, and save here.
         // Use the "future" level to compress the current returned
         // sample

         // Write new values to the samples: left

         left = _leftFifo.Push( left );
         left = (left * _correction) + _denormalFix;

         // Now the right!

         right = _rightFifo.Push( right );
         right = (right * _correction) + _denormalFix;

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
            return _LogDecayToSeconds( _compressAttackRate );
         }
         set
         {
            if (value < Denormal.denormalFixValue)
            {
               throw new ArgumentException( "Attack too small",
                                            "compressAttack" );
            }

            _compressAttackRate = _SecondsToLogDecay( value );
         }
      }

      ///
      /// Get/set the compress decay rate in seconds. 
      /// Decay time is defined as the time for the level to reach some
      /// percentage of the decay curve, which I don't feel like 
      /// researching just now.
      ///
      public double compressDecay
      {
         get
         {
            // Compute amount of decay per second in (decibels / 20.0)
            return _LogDecayToSeconds( _compressReleaseRate );
         }
         set
         {
            if (value <= 0.0)
               throw new ArgumentException( "Must be >= 0", "compressDecay" );

            // Divide by 72dB delay, multiply by 20 (because we don't bother
            // with the * -20 in the decibel calculation) and
            // divide by 44100 samples / second rate.
            _compressReleaseRate = _SecondsToLogDecay( value );
         }
      }

      ///
      /// Convert the input value into a number suitable for use in
      /// decay functions (say, compression).
      ///
      static double _SecondsToLogDecay( double input )
      {
         return (1.0 / input) * (3.0 / 20.0) / 44100.0;
      }
      
      static double _LogDecayToSeconds( double input )
      {
         return 1.0 / (input * 44100.0 / (3.0 / 20.0));
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
            _targetPowerLevel = value;
            
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
            _gateLevel = value;
            
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
      /// Start with something not too unusual
      ///
      double _targetPowerLevel = 2800.0;

      bool _learnTargetPower;

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
      // gain correction as used in the current & previous sample
      //
      double _correction = 1.0; 

      double _compressAttackRate = 1.0; // in LogDecay format
      double _compressReleaseRate = 1.0;

      // This is log10 of the average of the left/right average power
      double _avgPowerLog = Math.Log10( 32767.0 );

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
