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

   // using byteheaven.tamjb.Interfaces;

   ///
   /// A stereo single-band compressor with soft compression and simple
   /// (but not great) limiting. It's like a decent single channel of
   /// compression.
   ///
   public class PoorCompressor : IAudioProcessor, IMultiBandCompressor
   {
      public PoorCompressor()
      {
         // Output goes 
         // compressor -> limiter -> soft clipper -> gain
         // TO get a -0dB output level, we need the gain to 
         // make up for the limiter's peak output minus the gain
         // of the soft clipper at that level:
         _limiter.limit = 32767.0;
         _softClipper.clipThreshold = 18000.0;

         double peak = _softClipper.SoftClip( _limiter.limit );
         
         // Leave some headroom for roundoff error. And logic error. :)
         peak = peak + 10;

         _gain = 32767.0 / peak;


         // Set a nominal target level, using the default compression
         // and limiting setting above.
         compressThresholdBass = 12000;
         _compress.compressAttack = 0.40;
         _compress.compressPredelay = 40; // milliseconds
         _compress.compressDecay = 8.0; // I like 8, anything else is for testing
      }

      public bool doAutomaticLeveling
      {
         get
         {
            return _compress.doAutomaticLeveling;
         }
         set
         {
            _compress.doAutomaticLeveling = value;
         }
      }

      public void Process( ref double left,
                           ref double right )
      {
         _compress.Process( ref left, ref right );

         // Limit before the clipper so as to limit the max distortion, but
         // allow some distortion before it kicks in.

         _limiter.Process( ref left, ref right );

         _softClipper.Process( ref left, ref right );

         left *= _gain;
         right *= _gain;
      }

      public double compressAttack
      {
         get
         {
            return _compress.compressAttack;
         }
         set
         {
            _compress.compressAttack = value;
         }
      }

      public double compressDecay
      {
         get
         {
            return _compress.compressDecay;
         }
         set
         {
            _compress.compressDecay = value;
         }
      }

      public int compressThresholdBass
      {
         get
         {
            return _compress.compressThreshold;
         }
         set
         {
            _compress.compressThreshold = value;
         }
      }

      public int compressThresholdMid
      {
         get
         {
            return _compress.compressThreshold;
         }
         set
         {
            // do nothing
         }
      }

      public int compressThresholdTreble
      {
         get
         {
            return _compress.compressThreshold;
         }
         set
         {
            // do nothing
         }
      }

      public int gateThreshold
      {
         get
         {
            return _compress.gateThreshold;
         }
         set
         {
            _compress.gateThreshold = value;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public double compressRatio
      {
         get
         {
            return _compress.compressRatio;
         }
         set
         {
            _compress.compressRatio = value;
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
            return _compress.compressPredelay;
         }
         set
         {
            _compress.compressPredelay = value;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      /// The default is the only setting that sounds good.
      /// There is no reason to adjust
      /// this, except perhaps to turn it off. For which there
      /// is no current method. Hmmm.
      ///
      public int clipThreshold
      {
         get
         {
            return (int)Math.Round( _softClipper.clipThreshold );
         }
         set
         {
            _softClipper.clipThreshold = (double)value;
         }
      }

      Compressor _compress = new Compressor();
      SoftClipper _softClipper = new SoftClipper();
      Limiter _limiter = new Limiter();

      ///
      /// Output gain after processing (to make up for limiting loss)
      ///
      double _gain;
   }
}
