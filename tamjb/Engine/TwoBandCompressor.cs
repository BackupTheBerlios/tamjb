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
   /// too aggressive). With soft clipping based on antilog (1/x)
   /// based nonlinearity. ("Asympote!" "What did you just call me?")
   ///
   public class TwoBandCompressor
      : IAudioProcessor
   {
      ///
      /// Maximum value for the compressPredelay value. (samples)
      ///
      public readonly static uint MAX_PREDELAY = 88;

      public TwoBandCompressor()
      {
      }

      double crossoverFrequency
      {
         get
         {
            return _lowPass.cutoff;
         }
         set
         {
            _lowPass.cutoff = value;
            _highPass.cutoff = value;
         }
      }

      public void Process( ref double left,
                           ref double right )
      {
         // Split low and high, 
         // compress
         // Mix back together

         double bassLeft = left;
         double bassRight = right;
         double trebleLeft = left;
         double trebleRight = right;

         _lowPass.Process( ref bassLeft, ref bassRight );
         _bassCompress.Process( ref bassLeft, ref bassRight );

         _highPass.Process( ref trebleLeft, ref trebleRight );
         _trebleCompress.Process( ref trebleLeft, ref trebleRight );

         // Is this an adequate mixing algorithm?
         left = bassLeft + trebleLeft;
         right = bassRight + trebleRight;

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
            _trebleCompress.compressDecay = value;
         }
      }

      public int compressThreshold
      {
         get
         {
            return _bassCompress.compressThreshold;
         }
         set
         {
            _bassCompress.compressThreshold = value;
            _trebleCompress.compressThreshold = (int)((double)value * 0.8); // just a little less
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
            _trebleCompress.compressPredelay = value;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
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

      SoftClipper _softClipper = new SoftClipper();
      Compressor _bassCompress = new Compressor();
      Compressor _trebleCompress = new Compressor();
      LowpassFilter _lowPass = new LowpassFilter();
      HighpassFilter _highPass = new HighpassFilter();
   }
}
