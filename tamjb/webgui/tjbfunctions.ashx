<%@ WebHandler language="C#" Class=byteheaven.tamjb.webgui.TJBFunctions %>

namespace byteheaven.tamjb.webgui
{
   using System;
   using System.Diagnostics;
   using System.Text;
   using System.Threading;
   using System.Web;

   using Jayrock.Json;
   using Jayrock.JsonRpc;
   using Jayrock.JsonRpc.Web;

   using byteheaven.tamjb.Interfaces;

   public class StatusInfo
   {
      public StatusInfo()
      {
         userID = 0;
         nowPlaying = null;
      }

      public StatusInfo( uint id )
      {
         userID = id;
         this.nowPlaying = null;
      }

      public uint userID;
      public uint moodID;
      public string moodName;
      public ITrackInfo nowPlaying;
      public int suckPercent = 100;
      public int moodPercent = 0;
   }


   public class TJBFunctions : JsonRpcHandler
   {
      ///
      /// Gets the current status as a StatusInfo struct.
      ///
      [ JsonRpcMethod("getStatus") ]
      public StatusInfo GetStatus()
      {
         try
         {
            _Authenticate();

            return _MakeStatus( WebPageBase.backend );
         }
         catch (Exception ex)
         {
            _Trace( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("suckLess") ]
      public StatusInfo OnSuckLess( int trackId )
      {
         try 
         {
            Console.WriteLine( "[OnSuckLess] {0}", trackId );

            _Authenticate();

            IEngine backend = WebPageBase.backend;
            backend.DecreaseSuckZenoStyle( _userId, (uint)trackId );
            return _MakeStatus( backend );
         }
         catch (Exception ex)
         {
            _Trace( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("suckMore") ]
      public StatusInfo OnSuckMore( int trackId )
      {
         try 
         {
            Console.WriteLine( "[OnSuckMore] {0}", trackId );

            _Authenticate();

            IEngine backend = WebPageBase.backend;
            backend.IncreaseSuckZenoStyle( _userId, (uint)trackId );
            return _MakeStatus( backend );
         }
         catch (Exception ex)
         {
            _Trace( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("megaSuck") ]
      public StatusInfo OnMegaSuck( int trackId )
      {
         try 
         {
            Console.WriteLine( "[OnMegaSuck] {0}", trackId );

            _Authenticate();

            IEngine backend = WebPageBase.backend;
            for (int i = 0; i < 3; i++)
            {
               backend.IncreaseSuckZenoStyle( _userId, 
                                              (uint)trackId );
            }

            // Unconditionally go to the next track
            backend.GotoNextFile( _userId, (uint)trackId );
            return _MakeStatus( backend );
         }
         catch (Exception ex)
         {
            _Trace( ex.ToString() );
            throw;
         }
      }

      ///
      /// Sets the suck and mood values for the supplied status structure
      /// by querying the back end. Assumes status.nowPlaying.key is set.
      ///
      void _GetSuckAndMood( StatusInfo status )
      {
         double suckPercent;
         double moodPercent;
         WebPageBase.backend.GetAttributes( status.userID,
                                            status.moodID,
                                            status.nowPlaying.key,
                                            out suckPercent,
                                            out moodPercent );

         suckPercent /= 100;
         status.suckPercent = (int)suckPercent;

         moodPercent /= 100;
         status.moodPercent = (int)moodPercent;
      }


      ///
      /// Helper for all the functions that return current status.
      ///
      StatusInfo _MakeStatus( IEngine backend )
      {
         EngineState engineState = backend.GetState();
         StatusInfo status = new StatusInfo( _userId );
         
         Mood currentMood = new Mood();
         WebPageBase.backend.GetCurrentMood( _userId, ref currentMood );
         status.moodID = currentMood.id;
         status.moodName = currentMood.name;

         if (engineState.currentTrackIndex < 0)
            status.nowPlaying = null;
         else
            status.nowPlaying = engineState.currentTrack;

         _GetSuckAndMood( status );

         return status;
      }

      ///
      /// Confirms that the user is logged in, and initialized
      /// user ID and other user state from the authentication
      /// info.
      ///
      void _Authenticate()
      {
         System.Security.Principal.IIdentity identity = 
            HttpContext.Current.User.Identity;
 
         if (! identity.IsAuthenticated)
         {
            throw new ApplicationException( "Not authenticated" );
         }

         // If we got here, "It's cool, man"!
         _userId = Convert.ToUInt32( identity.Name );
      }


      void _Trace( string msg )
      {
         Trace.WriteLine( "tjbfunctions.ashx: " + msg ) ;
      }

      // Variables initialized in _Authenticate()
      uint _userId = 0;
   }
}