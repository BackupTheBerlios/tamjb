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

   public class HistoryInfo
   {
      public HistoryInfo()      // empty for Jayrock?
      {
      }

      public HistoryInfo( int key,
                          string title,
                          string artist,
                          string album,
                          int suck,
                          int mood )
      {
         this.key = key;
         this.title = title;
         this.artist = artist;
         this.album = album;
         this.suck = suck;
         this.mood = mood;
      }
      public int key;
      public string title;
      public string artist;
      public string album;
      public int suck;
      public int mood;

      ///
      /// OK, MISSING... etc?
      ///
      public string status;

      ///
      /// Probability a track will play
      ///
      public string prob;
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
      /// \param oldChangeCount is the change count the client currently
      /// has. Pass -1 as oldChangeCount to force refresh, otherwise will return
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

         // Console.WriteLine( "NewMood: {0}", moodID );

         WebPageBase.backend.SetMood( _userId, (uint)moodID );
        
         // Here's yet another thing that should check that the track hasn't
         // changed since the user set his mood: the mood changed, see if this
         // track is appropriate to this mood.
         WebPageBase.backend.ReevaluateCurrentTrack();

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
            Console.WriteLine( "TICKET: {0}", ticket.ToString() );
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

      [ JsonRpcMethod("createMood") ]
      public int CreateMood( string moodName )
      {
         WebPageBase.Authenticate( out _userId );
         Mood newMood = WebPageBase.backend.CreateMood( _userId, moodName );
         return (int)newMood.id;
      }

      [ JsonRpcMethod("deleteMood") ]
      public void DeleteMood( int moodId )
      {
         WebPageBase.Authenticate( out _userId );
         WebPageBase.backend.DeleteMood( _userId, (uint)moodId );
      }

      ///
      /// Gets the history status as an array of thingumies.
      ///
      /// \param when 'past' or 'future' (or maybe 'present' someday)
      /// \param moodID the listener's mood id
      ///
      [ JsonRpcMethod("getHistory") ]
      public HistoryInfo [] GetHistory( string when, int moodID )
      {
         WebPageBase.Authenticate( out _userId );

         EngineState state = WebPageBase.backend.GetState();

         // Figure out the first track, last track, and count. (Need the count
         // before we start emitting json!)

         int index;             // current track counter
         int lastIndex;
         if ("past" == when)
         {
            index = 0;
            lastIndex = state.currentTrackIndex;
         }
         else if ("future" == when)
         {
            index = state.currentTrackIndex + 1;
            lastIndex = state.playQueue.Length;
         }
         else
         {
            throw new ArgumentException( "Must be 'past' or 'future'", 
                                         "when" );
         }

         if (index < 0)         // not playing?
            index = 0;

         int count = lastIndex - index;
         if (count <= 0)
         {
            count = 0;
         }

         HistoryInfo [] history = new HistoryInfo[count];

         for (int i = 0; i < count; i++, index++)
         {
            if (index >= lastIndex)
            {
               throw new ApplicationException( 
                  "Internal error: index > history size" );
            }

            ITrackInfo info  = state.playQueue[index];

            int suck;
            int mood;
            _GetAttributes( info.key, (uint)moodID, out suck, out mood );
            
            HistoryInfo trackHistory = 
               new HistoryInfo(                  
                  (int)info.key,
                  info.title,
                  info.artist,
                  info.album,
                  suck,
                  mood );
            
            if (TrackStatus.MISSING == info.status)
            {
               trackHistory.status = "MISSING";
               trackHistory.title += " (MISSING)";
            }
            else
            {
               trackHistory.status = info.evaluation.ToString();
            }
               
            // What are the chances this will play? Divide into 5 ranges.
            // Something below 10% or above 90% never plays, so that's 
            // intervals of 16%:
               
            int prob = (100 - suck) * mood;
            if (prob < (26 * 26))     // 10 * 10 = 100 - never play
               trackHistory.prob = "probLow";
            else if (prob < (42 * 42))
               trackHistory.prob = "probMedLow";
            else if (prob < (58 * 58))
               trackHistory.prob = "probMed";
            else if (prob < (74 * 74))
               trackHistory.prob = "probMedHigh";
            else
               trackHistory.prob = "probHigh";

            history[i] = trackHistory;
         }

         return history;
      }


      ///
      /// Sets the suck and mood values for the supplied status structure
      /// by querying the back end. Assumes status.nowPlaying.key is set.
      ///
      void _GetSuckAndMood( StatusInfo status )
      {
         _GetAttributes( status.nowPlaying.key,
                         status.moodID,
                         out status.suckPercent,
                         out status.moodPercent );
      }

      void _GetAttributes( uint key, uint current_mood, out int suck, out int mood )
      {
         double suckPercent;
         double moodPercent;
         WebPageBase.backend.GetAttributes( _userId,
                                            current_mood,
                                            key,
                                            out suckPercent,
                                            out moodPercent );

         suckPercent /= 100;
         suck = (int)suckPercent;

         moodPercent /= 100;
         mood = (int)moodPercent;
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