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
   /// Represents a snapshot of the engine state at the time
   /// it is checked.
   ///
   public interface IEngineState
   {
      bool isPlaying{ get; }
      int currentTrackIndex{ get; }
      int unplayedTrackCount{ get; }
      ITrackInfo currentTrack{ get; }
      ITrackInfo this [int index]{ get; }
      int Count{ get; }

   }

   ///
   /// Interface to the tam player engine
   ///
   /// \see tam.Engine
   ///
   /// \todo Move the tam.engine docs into this file, because it
   ///   makes more sense for the documentation to be on the interface
   ///
   public interface IEngine
   {
      ICredentials GetUser( string name );

      IMood GetMood( ICredentials cred, string name );

      ///
      /// Return an array of user names that can be used to log in
      ///
      ICredentials [] GetUserList();

      ///
      /// Return the list of available moods
      ///
      IMood [] GetMoodList( ICredentials cred );

      ///
      /// Create new user, returns new user's credentials.
      ///
      /// \throw exception if already exists, etc.
      ///
      ICredentials CreateUser( string name );

      ///
      /// Create and return a new mood for this user
      ///
      IMood CreateMood( ICredentials cred, string name );

      ///
      /// Initialize or renew a logon using existing credentials
      ///
      void RenewLogon( ICredentials cred );

      ///
      /// Set current mood for a logged-on user
      ///
      void SetMood( ICredentials cred, IMood mood );

      /// 
      /// Figure out who is currently in control,a nd what they think
      ///
      void GetCurrentUserAndMood( out ICredentials cred,
                                  out IMood mood );

      ///
      /// Get a snapshot of the engine state. 
      ///
      /// \param state May be null. If state has changed, it will be
      ///   assigned a new value in this call.
      ///
      /// \return true if anything has changed since the last call
      ///
      bool CheckState( ref IEngineState state );

      // These are fine - they retrieve info about tracks and playlist 
      // criteria using the unique keys.
      ITrackInfo GetFileInfo( uint key );

      void GetAttributes( ICredentials cred,
                          IMood mood,
                          uint trackKey,
                          out double suck,
                          out double appropriate );

      // These could possibly be changed to indicate the percieved 
      // "current value" of the attribute, so the server can be more
      // intelligent.
      void IncreaseSuckZenoStyle( ICredentials cred,
                                  uint trackKey );

      void DecreaseSuckZenoStyle( ICredentials user,
                                  uint trackKey );

      void IncreaseAppropriateZenoStyle( ICredentials user,
                                         IMood mood,
                                         uint trackKey );

      void DecreaseAppropriateZenoStyle( ICredentials user,
                                         IMood mood,
                                         uint trackKey );


      // These need to take, as a parameter, some sort of clue as to 
      // what your application's  state is, to deal with multiple 
      // concurrent requests.
      void GotoNextFile( ICredentials user, uint currentTrackKey );
      void GotoPrevFile( ICredentials user, uint currentTrackKey );

      ///
      /// Stops playback if started
      ///
      void StopPlaying();

      ///
      /// Starts playback if stopped
      ///
      void StartPlaying();

      //
      // Compression parameters
      //
      double compressAttack { get; set; }
      double compressDecay { get; set; }
      int compressThreshold { get; set; }
      double compressRatio{ get; set; }

      int gateThreshold{ get; set; }

      ///
      /// SOFT clip threshold.
      ///
      int clipThreshold{ get; set; }

      
   }
}
