/// \file
///
/// $Id$
/// A test program for the ID3v2 reader. Don't expect it to be bug-free,
/// up-to-date, useful, or even valid c-sharp code.
///

// Copyright (C) 2004 Tom Surace.
//
// This file is part of the byteheaven.id3 package.
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

namespace byteheaven.id3
{
   using System;
   using System.Diagnostics;
   using System.IO;
   using System.Collections;
   using System.Collections.Specialized;

   ///
   /// A helper class to retrieve bytes from the mpeg stream intelligently.
   ///
   public class TestProgram
   {
      static void _Usage()
      {
         Console.WriteLine( "Usage: id3helper <options> <--> filename <...>" );
         Console.WriteLine( " --verbose     Be verbose (repeat for more)" );
         Console.WriteLine( " -v" );
         Console.WriteLine( " --list        List all tags found in file" );
         Console.WriteLine( " -l" );
      }

      public static int Main( string [] args )
      {
         int verbosity = 0;
         bool noMoreOptions = false;
         bool doListAction = false;

         ArrayList fileList = new ArrayList();

         foreach (string arg in args)
         {
            if (noMoreOptions)
            {
               fileList.Add( arg );
            }
            else
            {
               switch (arg)
               {
               case "-v":
               case "--verbose":
                  ++ verbosity;
                  break;

               case "-l":
               case "--list":
                  doListAction = true;
                  break;
               
               default:
                  fileList.Add( arg );
                  break;
               }
            }
         }

         // Dump trace output if verbose
         if (verbosity > 0)
         {
            Trace.Listeners.Add( new TextWriterTraceListener(Console.Out) );
            Trace.AutoFlush = true;
         }

         foreach (string file in fileList)
         {
            ID3v2 tag = null;
            try
            {
               bool dumpDebugInfo = false;
               if (verbosity > 2)
                  dumpDebugInfo = true;

               tag = new ID3v2( file, dumpDebugInfo );
               ID3v2Header header = tag.header;
               if (header == null)
               {
                  Console.WriteLine( "File: {0}", file );
                  Console.WriteLine( "No ID3v2 header found" );
                  continue;     // ** Next File Please **
               }
               
            }
            catch (Exception e)
            {
               Console.WriteLine( "File: {0}", file );
               Console.WriteLine( e.ToString() );
               return 1;
            }

            if (verbosity > 0)
            {
               Console.WriteLine( "File: {0}", file );
               Console.WriteLine( "Found Tag: ID3v2.{0}", 
                                  tag.header.version );
            }

            if (verbosity > 1)
            {
               Console.WriteLine( "Size: {0}", tag.header.size );
               Console.WriteLine( "IsUnsynchronized: {0}",
                                  tag.header.isUnsynchronized );
               Console.WriteLine( "HasExtHeader: {0}", 
                                  tag.header.hasExtendedHeader );
               Console.WriteLine( "IsExperimental: {0}", 
                                  tag.header.isExperimental );
               Console.WriteLine( "HasFooter: {0}", 
                                  tag.header.hasFooter );
            }

            if (verbosity > 0)
            {
               if (tag.tit2 != null)
                  Console.WriteLine( "TIT2: {0}", tag.tit2 );

               if (tag.tpe1 != null)
                  Console.WriteLine( "TPE1: {0}", tag.tpe1 );

               if (tag.trck != null)
                  Console.WriteLine( "TRCK: {0}", tag.trck );

               if (tag.comm != null)
                  Console.WriteLine( "TCON: {0}", tag.comm );

               if (tag.comm != null)
                  Console.WriteLine( "TYER: {0}", tag.tyer );

               if (tag.tcon != null)
                  Console.WriteLine( "TCON: {0}", tag.tcon );

               if (tag.mcdi != null)
                  Console.WriteLine( "MCDI found (what do I do with it?)" );

               if (tag.trackIndex > 0)
                  Console.WriteLine( "Index: {0}", tag.trackIndex );

               if (tag.trackCount > 0)
                  Console.WriteLine( "TrackCount: {0}", tag.trackCount );
            }
         }

         return 0;
      }
   }
}
