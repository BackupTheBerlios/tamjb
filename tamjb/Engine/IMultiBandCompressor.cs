/// \file
/// $Id$
///

// Copyright (C) 2006 Tom Surace.
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
   ///
   /// An interface for saving our multi band compression settings.
   ///
   public interface IMultiBandCompressor
   {
      bool doAutomaticLeveling { get; set; }
      double compressAttack{ get; set; }
      double compressDecay{ get; set; }
      int compressThresholdBass{ get; set; }
      int compressThresholdMid{ get; set; }
      int compressThresholdTreble{ get; set; }
      int gateThreshold{ get; set; }
      double compressRatio{ get; set; }
      uint compressPredelay{ get; set; }
      int clipThreshold{ get; set; }

   }
}
