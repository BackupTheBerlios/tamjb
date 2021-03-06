/// \file
/// $Id$
///
/// The jukebox player engine.
///

// Copyright (C) 2004-2008 Tom Surace.
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
   using System.Runtime.Remoting;
   using System.Runtime.Remoting.Lifetime; // for ILease

   using byteheaven.tamjb.Interfaces;

   ///
   /// A singlecall interface to the player back end. This is not
   /// the actual backend, which is Backend.
   ///
   /// \see Backend
   ///
   public class Engine 
      : MarshalByRefObject, IEngine
   {
      ///
      /// Constructor with no parameters is required for remoting under
      /// mono. Right now.
      ///
      public Engine()
      {
      }
      
      ~Engine()
      {
         // System.Runtime.Remoting.RemotingServices.Disconnect(this); ???
      }

      //
      // Get the current mood. May return null for either or both
      //
      public void GetCurrentMood( uint userId,
                                  ref Mood mood )
      {
         Backend.theBackend.GetCurrentMood( userId, ref mood );
      }

      public bool CheckState( ref EngineState state )
      {
         return Backend.theBackend.CheckState( ref state );
      }

      public EngineState GetState()
      {
         return Backend.theBackend.GetState();
      }

      public long changeCount
      {
         get
         {
            return Backend.theBackend.changeCount;
         }
      }

      ///
      /// Increases suck by 50% of the difference between its
      /// current level and 100%. Thus it theoretically never reaches
      /// 100% (cause integer math rounds down).
      ///
      public void IncreaseSuckZenoStyle( uint userId,
                                         uint trackKey )
      {
         Backend.theBackend.IncreaseSuckZenoStyle( userId, trackKey );
      }

      public void DecreaseSuckZenoStyle( uint userId,
                                         uint trackKey )
      {
         Backend.theBackend.DecreaseSuckZenoStyle( userId, trackKey );
      }

      public void IncreaseAppropriateZenoStyle( uint userId,
                                                uint moodId,
                                                uint trackKey )
      {
         Backend.theBackend.IncreaseAppropriateZenoStyle( userId, 
                                                          moodId, 
                                                          trackKey );
      }

      public void DecreaseAppropriateZenoStyle( uint userId,
                                                uint moodId,
                                                uint trackKey )
      {
         Backend.theBackend.DecreaseAppropriateZenoStyle( userId, 
                                                          moodId, 
                                                          trackKey );
      }

      public ITrackInfo GetFileInfo( uint key )
      {
         return Backend.theBackend.GetFileInfo( key );
      }

      ///
      /// IEngine interfaces
      ///
      public void GetAttributes( uint userId,
                                 uint moodId,
                                 uint trackKey,
                                 out double suck,
                                 out double appropriate )
      {
         Backend.theBackend.GetAttributes( userId,
                                           moodId,
                                           trackKey,
                                           out suck,
                                           out appropriate );
      }

      ///
      /// GotoFile function with credentials for future expansion.
      ///
      public void GotoNextFile( uint userId, uint currentTrackKey )
      {
         Backend.theBackend.GotoNextFile( userId, currentTrackKey );
      }

      public void GotoPrevFile( uint userId, uint currentTrackKey )
      {
         Backend.theBackend.GotoPrevFile( userId, currentTrackKey );
      }

      public void ReevaluateCurrentTrack()
      {
         Backend.theBackend.ReevaluateCurrentTrack( );
      }
   
      //
      // Attack ratio per-sample. Hmmm.
      //
      public double compressAttack
      {
         get
         {
            return Backend.theBackend.compressAttack;
         }
         set
         {
            Backend.theBackend.compressAttack = value;
         }
      }

      public double compressDecay
      {
         get
         {
            return Backend.theBackend.compressDecay;
         }
         set
         {
            Backend.theBackend.compressDecay = value;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public int compressThresholdBass
      {
         get
         {
            return Backend.theBackend.compressThresholdBass;
         }
         set
         {
            Backend.theBackend.compressThresholdBass = value;
         }
      }

      public int compressThresholdMid
      {
         get
         {
            return Backend.theBackend.compressThresholdMid;
         }
         set
         {
            Backend.theBackend.compressThresholdMid = value;
         }
      }

      public int compressThresholdTreble
      {
         get
         {
            return Backend.theBackend.compressThresholdTreble;
         }
         set
         {
            Backend.theBackend.compressThresholdTreble = value;
         }
      }

      public bool learnLevels 
      { 
         get
         {
            return Backend.theBackend.learnLevels;
         } 
         set
         {
            Backend.theBackend.learnLevels = value;
         }
      }


      ///
      /// Predelay (negative delay on attack)
      ///
      public uint compressPredelay
      {
         get
         {
            return Backend.theBackend.compressPredelay;
         }
         set
         {
            Backend.theBackend.compressPredelay = value;
         }
      }

      ///
      /// Predelay (negative delay on attack)
      ///
      public uint compressPredelayMax
      {
         get
         {
            return Backend.theBackend.compressPredelayMax;
         }
      }

      ///
      /// Input level gate threshold (0-32767). Probably should be
      /// less than the compress threshold. :)
      ///
      public int gateThreshold
      {
         get
         {
            return Backend.theBackend.gateThreshold;
         }
         set
         {
            Backend.theBackend.gateThreshold = value;
         }
      }

      ///
      /// Compress threshold as a 16-bit unsigned int. 
      ///
      public double compressRatio
      {
         get
         {
            return Backend.theBackend.compressRatio;
         }
         set
         {
            Backend.theBackend.compressRatio = value;
         }
      }

      ///
      /// Stop!
      ///
      public void StopPlaying()
      {
         Backend.theBackend.StopPlaying();
      }

      ///
      /// Starts playback if stopped
      ///
      public void StartPlaying()
      {
         Backend.theBackend.StartPlaying();
      }

      public bool isPlaying
      {
         get
         {
            return Backend.theBackend.isPlaying;
         }
      }

      ///
      /// Return the list of available moods (as Mood)
      ///
      public Mood [] GetMoodList( uint userId )
      {
         return Backend.theBackend.GetMoodList( userId );
      }

      public UserInfo [] GetUserList()
      {
         return Backend.theBackend.GetUserList();
      }

      public UserInfo CreateUser( string name, string password )
      {
         return Backend.theBackend.CreateUser( name, password );
      }

      public UserInfo LogIn( string name, string password )
      {
         return Backend.theBackend.LogIn( name, password );
      }

      ///
      /// Create and return a new mood for this user
      ///
      public Mood CreateMood( uint userId, string name )
      {
         return Backend.theBackend.CreateMood( userId, name );
      }

      public void DeleteMood( uint userId, uint moodId )
      {
         Backend.theBackend.DeleteMood( userId, moodId );
      }

      public UserInfo RenewLogon( uint userId )
      {
         return Backend.theBackend.RenewLogon( userId );
      }

      public void SetMood( uint userId, uint moodId )
      {
         Backend.theBackend.SetMood( userId, moodId );
      }

      ///
      /// Get an existing mood by name for a given user
      ///
      public Mood GetMood( uint userId, string name )
      {
         return Backend.theBackend.GetMood( userId, name );
      }

      ///
      /// Get the credentials for this user
      ///
      public UserInfo GetUser( string name )
      {
         return Backend.theBackend.GetUser( name );
      }

      public UserInfo GetUser( uint uid )
      {
         return Backend.theBackend.GetUser( uid );
      }

   }
}
