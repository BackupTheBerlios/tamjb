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
   public class SoftClipper : SoftKneeDevice, IAudioProcessor
   {

      public void Process( ref double left, ref double right )
      {
         left = SoftClip( left );
         right = SoftClip( right );
      }
   }
}
