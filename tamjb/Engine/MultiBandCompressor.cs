/// \file
/// $Id$
///

// Copyright (C) 2004-2008 Tom Surace.
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
      : IAudioProcessor, IMultiBandCompressor
   {
      public MultiBandCompressor( Backend.Quality quality )
      {
         switch (quality)
         {
         case Backend.Quality.HIGH:
            _crossover =
               new StereoCrossover( 190.0, 
                                    2200.0,
                                    StereoCrossover.Quality.HIGH );
            break;

         case Backend.Quality.MEDIUM:
            _crossover =
               new StereoCrossover( 190.0, 
                                    2200.0,
                                    StereoCrossover.Quality.MEDIUM );
            break;

         case Backend.Quality.LOW:
            _crossover = 
               new StereoCrossover( 190.0, 
                                    2200.0,
                                    StereoCrossover.Quality.LOW );
            break;

         default:
            throw new ApplicationException( "Unexpected case in switch" );
         }


         // Default levels:
         compressThresholdBass = 9000;    // 12000
         compressThresholdMid = 5775;     //  7000
         compressThresholdTreble = 3375;  //  4500

         // Allow overshoot in the limiter, as long as we are using
         // soft clipping.
         _limiter.limit = 49150.0;
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

         _crossover.Process( ref left, ref right,
                             out bassLeft, out bassRight,
                             out midLeft,  out midRight,
                             out trebleLeft, out trebleRight );

         _bassCompress.Process( ref bassLeft, ref bassRight );

         // Limit the bass, because it is most likely
         // to overshoot. Should make clipping less audible. 
         // _limiter.Process( ref bassLeft, ref bassRight );
         
         _midCompress.Process( ref midLeft, ref midRight );
         _trebleCompress.Process( ref trebleLeft, ref trebleRight );

         // Is this an adequate mixing algorithm? Hehe.
         left = bassLeft + midLeft + trebleLeft;
         right = bassRight + midRight + trebleRight;

         // Soft clipping and limiting together rules, but...at what cost?
         // Also...the limiter should allow some overshoot if we are using
         // soft clipping...
         _limiter.Process( ref left, ref right );
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
            return (int)Math.Round( _softClipper.clipThreshold );
         }
         set
         {
            _softClipper.clipThreshold = (double)value;
         }
      }

      StereoCrossover _crossover;

      Compressor _bassCompress = new Compressor();
      Compressor _midCompress = new Compressor();
      Compressor _trebleCompress = new Compressor();

      SoftClipper _softClipper = new SoftClipper();

      Limiter _limiter = new Limiter();
   }
}
