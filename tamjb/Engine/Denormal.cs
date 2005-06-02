/// \file
/// $Id$
///

// Copyright (C) 2005 Tom Surace.
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

   ///
   /// Helper code to detect and fix denormals.
   ///
   public class Denormal
   {

      ///
      /// Default good value for a simple square-wave denormalization
      /// process. 
      ///
      public static readonly double denormalFixValue = 1.0E-25;

      ///
      /// Prints to the console if the varibal "var" is getting small
      /// enough to worry about. Prints "what" as part of the message
      /// 
      public static void CheckDenormal( string what, double var )
      {
         // Actually, the denormal threshold is a bit lower than the
         // value we're using to prevent it:

         if (var < 1.0e-34 &&
             var > -1.0e-34 && 
             var != 0.0)
         {
            Trace.WriteLine( "DENORMAL : " + what + " : " + var.ToString() );
         }
      }
   }
}
