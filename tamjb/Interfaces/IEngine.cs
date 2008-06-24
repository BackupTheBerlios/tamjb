/// \file
/// $Id$
///
/// Jukebox backend "Engine" base type for remoting
///

// Copyright (C) 2004-2007 Tom Surace.
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

namespace byteheaven.tamjb.Interfaces
{
   ///
   /// Interface to the tam player engine
   ///
   /// \see tam.Engine
   ///
   /// \todo Move the Engine/Backend docs into this file, because it
   ///   makes more sense for the documentation to be on the interface
   ///
   public interface IEngine
   {
      ///
      /// Find user by name (name is unique, so this will work).
      ///
      UserInfo GetUser( string name );

      ///
      /// Find all info for this uid
      ///
      UserInfo GetUser( uint uid );

      ///
      /// Find User Info by logging in. Adds the user as a current controller
      /// on success.
      ///
      UserInfo LogIn( string name, string password );

      Mood GetMood( uint userId, string name );

      ///
      /// Return info for all logged-in users.
      ///
      UserInfo [] GetUserList();

      ///
      /// Return the list of available moods for this user.
      ///
      Mood [] GetMoodList( uint userId );

      ///
      /// Create new user, returns new user's info.
      ///
      /// \throw exception if already exists, etc.
      ///
      UserInfo CreateUser( string name, string password );

      ///
      /// Create and return a new mood for this user
      ///
      Mood CreateMood( uint userId, string name );

      ///
      /// Reset a logon so it won't time out. we hope!
      ///
      /// \return info if the logon was renewed, null if it timed out,
      ///   or never existed!
      ///
      UserInfo RenewLogon( uint userId );

      ///
      /// Set current mood for a logged-on user
      ///
      void SetMood( uint userId, uint moodId );

      /// 
      /// Get the current mood for this user. Assumes the user is logged in.
      ///
      void GetCurrentMood( uint userId,
                           ref Mood mood );

      ///
      /// Get a snapshot of the engine state. 
      ///
      /// \param state Not null. If state has changed, it will be
      ///   modified.
      ///
      /// \return true if anything has changed since the last call
      ///
      bool CheckState( ref EngineState state );

      ///
      /// This is incremented for each change to the back end. If this
      /// number has not changed, you can safely assume nothing has changed.
      ///
      /// \note change count is also returned as an element of 
      ///  the engine state for your comfort and convenience.
      ///
      long changeCount { get; }

      ///
      /// Gets the current engine state
      ///
      EngineState GetState();

      // These are fine - they retrieve info about tracks and playlist 
      // criteria using the unique keys.
      ITrackInfo GetFileInfo( uint key );

      void GetAttributes( uint userId,
                          uint moodId,
                          uint trackKey,
                          out double suck,
                          out double appropriate );

      // These could possibly be changed to indicate the percieved 
      // "current value" of the attribute, so the server can be more
      // intelligent.
      void IncreaseSuckZenoStyle( uint userId,
                                  uint trackKey );

      void DecreaseSuckZenoStyle( uint userId,
                                  uint trackKey );

      void IncreaseAppropriateZenoStyle( uint userId,
                                         uint moodId,
                                         uint trackKey );

      void DecreaseAppropriateZenoStyle( uint userId,
                                         uint moodId,
                                         uint trackKey );

      /// Forces the backend to reconsider the currently playing track.
      /// Which gives it another chance of being randomly removed. Useful
      /// after increasing the suck value (if you don't want to FORCE
      /// the next track, that is.
      void ReevaluateCurrentTrack();


      // These need to take, as a parameter, some sort of clue as to 
      // what your application's  state is, to deal with multiple 
      // concurrent requests.
      void GotoNextFile( uint userId, uint currentTrackKey );
      void GotoPrevFile( uint userId, uint currentTrackKey );

      ///
      /// Stops playback if started
      ///
      void StopPlaying();

      ///
      /// Starts playback if stopped
      ///
      void StartPlaying();

      ///
      /// Is it currently plyaing?
      ///
      bool isPlaying { get; }

      //
      // Compression parameters for the multi-band 
      // compressor.
      //
      double compressAttack { get; set; }
      double compressDecay { get; set; }
      int compressThresholdBass { get; set; }
      int compressThresholdMid { get; set; }
      int compressThresholdTreble { get; set; }
      bool learnLevels { get; set; }
      double compressRatio{ get; set; }
      uint compressPredelay{ get; set; }
      uint compressPredelayMax{ get; }

      int gateThreshold{ get; set; }

   }
}
