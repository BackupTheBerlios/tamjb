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

   ///
   /// Structure returned (on success) to the javascript calls that
   /// describes the current now-playing track and user status.
   ///
   public class StatusBase
   {
      public StatusBase( bool changed )
      {
         statusChanged = changed;
      }

      public bool statusChanged;
   }

   public class NoChangeStatus : StatusBase
   {
      public NoChangeStatus()
         : base(false)
      {
      }
   }

   public class StatusInfo : StatusBase
   {
      public StatusInfo()
         : base(true)
      {
         nowPlaying = null;
      }

      public uint moodID;
      public string moodName;
      public ITrackInfo nowPlaying;
      public int suckPercent = 100;
      public int moodPercent = 0;
      public long changeCount = -1;
   }

   ///
   /// Jayrock interface to T.A.M. Jukebox
   ///
   /// Thought: all public entry points shoul dhave try/catch wrappers
   /// that _Trace exceptions on the backend, for debugging.
   ///
   public class TJBFunctions : JsonRpcHandler
   {
      ///
      /// Gets the current status as a StatusBase struct.
      /// pass -1 as oldChangeCount to force refresh, otherwise will return
      /// a "nothing changed" status if the index has not changed.
      ///
      [ JsonRpcMethod("getStatus") ]
      public StatusBase GetStatus( int oldChangeCount )
      {
         try
         {
            IEngine backend = WebPageBase.backend;
            long newChangeCount = backend.changeCount;
            if (oldChangeCount == newChangeCount)
               return new NoChangeStatus();

            try 
            {
               _Authenticate();
               return _MakeStatus( WebPageBase.backend );
            }
            catch 
            {
               StatusInfo status = new StatusInfo();
               EngineState engineState = WebPageBase.backend.GetState();
               if (engineState.currentTrackIndex < 0)
                  status.nowPlaying = null;
               else
                  status.nowPlaying = engineState.currentTrack;
               
               status.moodID = 0;
               status.moodName = "(unknown)";
               status.suckPercent = 0;
               status.moodPercent = 100;
               status.changeCount = newChangeCount;
               return status;
            }
         }
         catch (Exception ex)
         {
            _Trace( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("suckLess") ]
      public StatusBase OnSuckLess( int trackId )
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
      public StatusBase OnSuckMore( int trackId )
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
      public StatusBase OnMegaSuck( int trackId )
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

      [ JsonRpcMethod("moodYes") ]
      public StatusBase OnMoodYes( int trackId, int moodId )
      {
         try 
         {
            Console.WriteLine( "[OnMoodYes] {0}:{1}", moodId, trackId );

            _Authenticate();

            IEngine backend = WebPageBase.backend;
            
            backend.IncreaseAppropriateZenoStyle( _userId, 
                                                  (uint)trackId, 
                                                  (uint)moodId );

            return _MakeStatus( backend );
         }
         catch (Exception ex)
         {
            _Trace( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("moodNo") ]
      public StatusBase OnMoodNo( int trackId, int moodId )
      {
         try 
         {
            Console.WriteLine( "[OnMoodNo] {0}:{1}", moodId, trackId );

            _Authenticate();

            IEngine backend = WebPageBase.backend;
            backend.DecreaseAppropriateZenoStyle( _userId, 
                                                  (uint)trackId, 
                                                  (uint)moodId );

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
         WebPageBase.backend.GetAttributes( _userId,
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
      StatusBase _MakeStatus( IEngine backend )
      {
         EngineState engineState = backend.GetState();
         StatusInfo status = new StatusInfo();
         
         status.changeCount = backend.changeCount;
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
            throw new ApplicationException( "login" );
         }

         // If we got here, "It's cool, man"!
         _userId = Convert.ToUInt32( identity.Name );

         // And.. we'd like to stay logged in. OK?
         UserInfo userInfo = WebPageBase.backend.RenewLogon( _userId );
         if (null == userInfo)
         {
            throw new ApplicationException( "login" );
         }
      }

      void _Trace( string msg )
      {
         Trace.WriteLine( "tjbfunctions.ashx: " + msg ) ;
      }

      // Variables initialized in _Authenticate()
      uint _userId = 0;
   }
}