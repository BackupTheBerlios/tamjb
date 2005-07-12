/// \file
/// $Id$
///
/// Jukebox backend "Engine" base type for remoting
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
      Credentials GetUser( string name );

      Mood GetMood( Credentials cred, string name );

      ///
      /// Return an array of user names that can be used to log in
      ///
      Credentials [] GetUserList();

      ///
      /// Return the list of available moods
      ///
      Mood [] GetMoodList( Credentials cred );

      ///
      /// Create new user, returns new user's credentials.
      ///
      /// \throw exception if already exists, etc.
      ///
      Credentials CreateUser( string name );

      ///
      /// Create and return a new mood for this user
      ///
      Mood CreateMood( Credentials cred, string name );

      ///
      /// Initialize or renew a logon using existing credentials
      ///
      void RenewLogon( Credentials cred );

      ///
      /// Set current mood for a logged-on user
      ///
      void SetMood( Credentials cred, Mood mood );

      /// 
      /// Figure out who is currently in control, and what they think
      ///
      void GetCurrentUserAndMood( ref Credentials cred,
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

      // These are fine - they retrieve info about tracks and playlist 
      // criteria using the unique keys.
      ITrackInfo GetFileInfo( uint key );

      void GetAttributes( Credentials cred,
                          Mood mood,
                          uint trackKey,
                          out double suck,
                          out double appropriate );

      // These could possibly be changed to indicate the percieved 
      // "current value" of the attribute, so the server can be more
      // intelligent.
      void IncreaseSuckZenoStyle( Credentials cred,
                                  uint trackKey );

      void DecreaseSuckZenoStyle( Credentials user,
                                  uint trackKey );

      void IncreaseAppropriateZenoStyle( Credentials user,
                                         Mood mood,
                                         uint trackKey );

      void DecreaseAppropriateZenoStyle( Credentials user,
                                         Mood mood,
                                         uint trackKey );

      /// Forces the backend to reconsider the currently playing track.
      /// Which gives it another chance of being randomly removed. Useful
      /// after increasing the suck value (if you don't want to FORCE
      /// the next track, that is.
      void ReevaluateCurrentTrack();


      // These need to take, as a parameter, some sort of clue as to 
      // what your application's  state is, to deal with multiple 
      // concurrent requests.
      void GotoNextFile( Credentials user, uint currentTrackKey );
      void GotoPrevFile( Credentials user, uint currentTrackKey );

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
