/// \file
/// $Id$
///
/// A lame command-line mp3 player for testing.
///

// Copyright (C) 2004 Tom Surace.
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
//   Tom Surace <tekhedd@byteheaven.net>

using System;
using System.Threading;

using byteheaven.tamjb.SimpleMp3Player;

   public class CmdLine
   {
      public static void Main( string[] args )
      {
         try
         {
            Console.WriteLine( "Hello" );

            // Parse command line args
            if (args.GetLength(0) != 1)
            {
               throw new ApplicationException( "Bad command line args" );
            }
            
            string file = args[0];

            Player player = new Player();
         
            player.bufferSize = 44100 * 1;
            player.buffersInQueue = 20;
            player.buffersToPreload = 3;

            player.OnTrackFinished +=
               new TrackFinishedHandler( _TrackFinished );

            player.PlayFile( file, 1 );

            while (player.isPlaying)
            {
               // Wait for the file to finish playing?
               Thread.Sleep( 5000 );
            }

            player.ShutDown();
            Console.WriteLine( "Done..." );
         }
         catch (Exception e)
         {
            Console.WriteLine( e.ToString() );
         }

      }

      static void _TrackFinished( TrackFinishedInfo info )
      {
         Console.WriteLine( "TrackFinished: " + info.reason.ToString() );

         Exception e = info.exception;
         if (null != e)
            Console.WriteLine( e.ToString() );
      }
   }

