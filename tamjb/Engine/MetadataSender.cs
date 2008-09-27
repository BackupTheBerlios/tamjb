/// \file
/// $Id$
///
/// The jukebox player back end.
///

// Copyright (C) 2008 Tom Surace.
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
   using System.Collections;
   using System.Data;
   using System.Diagnostics;
   using System.IO;
   using System.Threading;
   using System.Xml.Serialization;

   using byteheaven.tamjb.Interfaces;
   using byteheaven.tamjb.SimpleMp3Player;

   ///
   /// A helper class that generates metadata for a streaming engine,
   /// by running an external program.
   ///
   internal class MetadataSender
   {
      internal MetadataSender( string programToRun )
      {
         _program = programToRun;
      }

      internal void Update( PlayableData nowPlaying )
      {
         if (String.Empty == _program) // not configured?
            return;

         Process metadataProcess =
            Process.Start( _program,
                           " artist='" + _Escape(nowPlaying.artist) +
                           "' title='" + _Escape(nowPlaying.title) +
                           "' album='" + _Escape(nowPlaying.album) + "'"
                           );

         if (!metadataProcess.WaitForExit( 10000 ))
         {
            // It's taking too long. Kill it.
            metadataProcess.Kill();
         }
      }

      ///
      /// Remove = and ' from the string for command-line processing,
      /// as a quick dirty hack.
      ///
      static string _Escape( string input )
      {
         string output = input.Replace( '\'', ' ' );
         output = output.Replace( '=', ' ' );

         return output;
      }

      string _program = String.Empty;
   }
}