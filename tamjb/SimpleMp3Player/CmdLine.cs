// $Id$

using System;
using System.Threading;

using tam.SimpleMp3Player;

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
         
            Player.bufferSize = 44100 * 1;
            Player.buffersInQueue = 20;
            Player.buffersToPreload = 3;

            Player.OnTrackFinished +=
               new TrackFinishedHandler( _TrackFinished );

            Player.PlayFile( file, 1 );

            while (Player.isPlaying)
            {
               // Wait for the file to finish playing?
               Thread.Sleep( 5000 );
            }

            Player.ShutDown();
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

