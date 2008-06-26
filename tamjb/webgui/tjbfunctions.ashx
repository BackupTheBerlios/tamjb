<%@ WebHandler language="C#" Class=byteheaven.tamjb.webgui.TJBFunctions %>

namespace byteheaven.tamjb.webgui
{
   using System;
   using System.Diagnostics;
   using System.Text;
   using System.Threading;
   using System.Web;
   using System.Web.Security;

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

      public string userName;
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
   /// that Console.WriteLine exceptions on the backend, for debugging.
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
            WebPageBase.Authenticate( out _userId ); // Don't get logged out!
            if (oldChangeCount == backend.changeCount)
               return new NoChangeStatus();

            try 
            {
               return _MakeStatus( WebPageBase.backend );
            }
            catch 
            {
               return _MakeAnonStatus( WebPageBase.backend );
            }
         }
         catch (Exception ex)
         {
            Console.WriteLine( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("suckLess") ]
      public StatusBase OnSuckLess( int trackId )
      {
         try 
         {
            Console.WriteLine( "[OnSuckLess] {0}", trackId );

            WebPageBase.Authenticate( out _userId );

            IEngine backend = WebPageBase.backend;
            backend.DecreaseSuckZenoStyle( _userId, (uint)trackId );
            return _MakeStatus( backend );
         }
         catch (Exception ex)
         {
            Console.WriteLine( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("suckMore") ]
      public StatusBase OnSuckMore( int trackId )
      {
         try 
         {
            Console.WriteLine( "[OnSuckMore] {0}", trackId );

            WebPageBase.Authenticate( out _userId );

            IEngine backend = WebPageBase.backend;
            backend.IncreaseSuckZenoStyle( _userId, (uint)trackId );

            EngineState engineState = backend.GetState();
            if (engineState.currentTrack.key == (uint)trackId)
               backend.ReevaluateCurrentTrack();

            return _MakeStatus( backend );
         }
         catch (Exception ex)
         {
            Console.WriteLine( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("megaSuck") ]
      public StatusBase OnMegaSuck( int trackId )
      {
         try 
         {
            Console.WriteLine( "[OnMegaSuck] {0}", trackId );

            WebPageBase.Authenticate( out _userId );

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
            Console.WriteLine( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("moodYes") ]
      public StatusBase OnMoodYes( int trackId, int moodId )
      {
         try 
         {
            Console.WriteLine( "[OnMoodYes] {0}:{1}", moodId, trackId );

            WebPageBase.Authenticate( out _userId );

            IEngine backend = WebPageBase.backend;

            backend.IncreaseAppropriateZenoStyle( _userId, 
                                                  (uint)moodId, 
                                                  (uint)trackId );

            return _MakeStatus( backend );
         }
         catch (Exception ex)
         {
            Console.WriteLine( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("moodNo") ]
      public StatusBase OnMoodNo( int trackId, int moodId )
      {
         try 
         {
            Console.WriteLine( "[OnMoodNo] {0}:{1}", moodId, trackId );

            WebPageBase.Authenticate( out _userId );

            IEngine backend = WebPageBase.backend;
            backend.DecreaseAppropriateZenoStyle( _userId, 
                                                  (uint)moodId, 
                                                  (uint)trackId );

            EngineState engineState = backend.GetState();
            if (engineState.currentTrack.key == (uint)trackId)
               backend.ReevaluateCurrentTrack();

            return _MakeStatus( backend );
         }
         catch (Exception ex)
         {
            Console.WriteLine( ex.ToString() );
            throw;
         }
      }

      [ JsonRpcMethod("setMood") ]
      public StatusBase SetMood( int moodID )
      {
         WebPageBase.Authenticate( out _userId );

         if (moodID < 0)
            throw new ArgumentException( "moodID" );

         Console.WriteLine( "NewMood: {0}", moodID );

         WebPageBase.backend.SetMood( _userId, (uint)moodID );
         return _MakeStatus( WebPageBase.backend );
      }

      [ JsonRpcMethod("login") ]
      public StatusBase Login( string id, string password )
      {
         IEngine backend = WebPageBase.backend;

         // If no id was supplied, don't bail.
         if (id.Length > 0)
         {
            UserInfo userInfo = backend.LogIn( id, password );
            if (null == userInfo)
               return _MakeAnonStatus( backend );

            _userId = userInfo.id;

            FormsAuthenticationTicket ticket = 
               new FormsAuthenticationTicket( 1, // version 1!
                                              userInfo.id.ToString(),
                                              DateTime.Now,   
                                              DateTime.Now.AddMinutes(10),
                                              true, // persistent
                                              userInfo.name, // user data for us
                                              FormsAuthentication.FormsCookiePath );

            // Encrypt the ticket.
            string encTicket = FormsAuthentication.Encrypt( ticket );

            // Create the cookie.
            Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, 
                                                encTicket));


            // Get the redirect url, but don't redirect! :)
            FormsAuthentication.GetRedirectUrl( userInfo.id.ToString(), 
                                                false );

            return _MakeStatus( backend );
         }

         return _MakeAnonStatus( backend );
      }

      [ JsonRpcMethod("logout") ]
      public void Logout() 
      {
         FormsAuthentication.SignOut();
         Session.Abandon();
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

         UserInfo userInfo = WebPageBase.backend.GetUser( _userId );
         status.userName = userInfo.name;
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
      /// Return a status structure suitable to a non-logged-in user
      ///
      /// userName is empty? That's your clue.
      ///
      StatusInfo _MakeAnonStatus( IEngine backend )
      {
         StatusInfo status = new StatusInfo();
         EngineState engineState = WebPageBase.backend.GetState();
         if (engineState.currentTrackIndex < 0)
            status.nowPlaying = null;
         else
            status.nowPlaying = engineState.currentTrack;
         
         status.userName = String.Empty;
         status.moodID = 0;
         status.moodName = String.Empty;
         status.suckPercent = 0;
         status.moodPercent = 100;
         status.changeCount = backend.changeCount;
         return status;
      }


      // Variables initialized in WebPageBase.Authenticate( out _userId )
      uint _userId = 0;
   }
}