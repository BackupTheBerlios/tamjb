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
   /// A multi-band compressor. Yeah. What a useful comment.
   ///
   public class MultiBandCompressor
      : IAudioProcessor
   {
      ///
      /// Maximum value for the compressPredelay value. (samples)
      ///
      public readonly static uint MAX_PREDELAY = 88;

      public MultiBandCompressor()
      {
         // Note: default crossover frequencies are related to
         // the compressThreshold adjustment!

         crossoverFrequencyOne = 220; // hz
         crossoverFrequencyTwo = 5800; // hz
      }

      public bool doAutomaticLeveling
      {
         get
         {
            return _bassCompress.doAutomaticLeveling;
         }
         set
         {
            _bassCompress.doAutomaticLeveling = value;
            _midCompress.doAutomaticLeveling = value;
            _trebleCompress.doAutomaticLeveling = value;
         }
      }

      public double crossoverFrequencyOne
      {
         get
         {
            return _lowPassOne.cutoff;
         }
         set
         {
            _lowPassOne.cutoff = value;
            _highPassOne.cutoff = value;
         }
      }

      public double crossoverFrequencyTwo
      {
         get
         {
            return _lowPassTwo.cutoff;
         }
         set
         {
            _lowPassTwo.cutoff = value;
            _highPassTwo.cutoff = value;
         }
      }

      public void Process( ref double left,
                           ref double right )
      {
         // Split low and high, 
         // compress
         // Mix back together

         double bassLeft;
         double bassRight;
         double midLeft;
         double midRight;
         double trebleLeft;
         double trebleRight;

         _crossover.Process( left, right,
                             out bassLeft, out bassRight,
                             out midLeft,  out midRight );

         _bassCompress.Process( ref bassLeft, ref bassRight );

         // Clip the bass separately, because it is most likely
         // to overshoot (this will create interesting
         // harmonics, won't it? :)

         _softClipper.Process( ref bassLeft, ref bassRight );

         _midCompress.Process( ref midLeft, ref midRight );

//          _highPassTwo.Process( ref trebleLeft, ref trebleRight );
//          _trebleCompress.Process( ref trebleLeft, ref trebleRight );

         // Is this an adequate mixing algorithm?
         left = bassLeft + midLeft;
         right = bassRight + midRight;

         _softClipper.Process( ref left, ref right );
      }

      public double compressAttack
      {
         get
         {
            return _bassCompress.compressAttack;
         }
         set
         {
            _bassCompress.compressAttack = value;
            _midCompress.compressAttack = value;
            _trebleCompress.compressAttack = value;
         }
      }

      public double compressDecay
      {
         get
         {
            return _bassCompress.compressDecay;
         }
         set
         {
            _bassCompress.compressDecay = value;
            _midCompress.compressDecay = value;
            _trebleCompress.compressDecay = value;
         }
      }

      public int compressThresholdBass
      {
         get
         {
            return _bassCompress.compressThreshold;
         }
         set
         {
            _bassCompress.compressThreshold = value;
         }
      }

      public int compressThresholdMid
      {
         get
         {
            return _midCompress.compressThreshold;
         }
         set
         {
            _midCompress.compressThreshold = value;
         }
      }

      public int compressThresholdTreble
      {
         get
         {
            return _trebleCompress.compressThreshold;
         }
         set
         {
            _trebleCompress.compressThreshold = value;
         }
      }

      public int gateThreshold
      {
         get
         {
            return _bassCompress.gateThreshold;
         }
         set
         {
            _bassCompress.gateThreshold = value;
            _midCompress.gateThreshold = value;
            _trebleCompress.gateThreshold = value;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public double compressRatio
      {
         get
         {
            return _bassCompress.compressRatio;
         }
         set
         {
            _bassCompress.compressRatio = value;
            _midCompress.compressRatio = value;
            _trebleCompress.compressRatio = value;
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
            return _bassCompress.compressPredelay;
         }
         set
         {
            _bassCompress.compressPredelay = value;
            _midCompress.compressPredelay = value;
            _trebleCompress.compressPredelay = value;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      /// The default is OK, there's no real reason to adjust
      /// this, except perhaps to turn it off. For which there
      /// is no current method. Hmmm.
      ///
      public int clipThreshold
      {
         get
         {
            return _softClipper.clipThreshold;
         }
         set
         {
            _softClipper.clipThreshold = value;
         }
      }

      StereoCrossover _crossover = new StereoCrossover( 220.0 );

      Compressor _bassCompress = new Compressor();

      LowpassFilter _lowPassOne = new LowpassFilter();
      HighpassFilter _highPassOne = new HighpassFilter();

      Compressor _midCompress = new Compressor();

      LowpassFilter _lowPassTwo = new LowpassFilter();
      HighpassFilter _highPassTwo = new HighpassFilter();

      Compressor _trebleCompress = new Compressor();

      SoftClipper _softClipper = new SoftClipper();
   }
}