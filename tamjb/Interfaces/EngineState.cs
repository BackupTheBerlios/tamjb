/// \file
/// $Id$
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

namespace byteheaven.tamjb.Engine
{
   using System;
   using System.Collections;

   using byteheaven.tamjb.Interfaces;

   ///
   /// Information about the state of the jukebox backend sent
   /// over the Remoting connection.
   ///
   [Serializable]
   public class EngineState : IEngineState
   {
      bool      _isPlaying;
      int       _currentTrackIndex;
      ArrayList _playQueue; // What's coming up, and what we've played.
      long      _changeCount;

      public EngineState( bool isPlaying,
                          int  currentTrackIndex,
                          ArrayList playQueue,
                          long changeCount )
      {
         _isPlaying = isPlaying;
         _currentTrackIndex = currentTrackIndex;
         _playQueue = playQueue; // should make a copy instead of ref?
         _changeCount = changeCount;
      }

      public long changeCount
      {
         get
         {
            return _changeCount;
         }
      }

      public bool isPlaying
      { 
         get
         {
            return _isPlaying;
         }
      }

      public int currentTrackIndex
      { 
         get
         {
            return _currentTrackIndex;
         }
      }

      public int unplayedTrackCount
      { 
         get
         {
            return _playQueue.Count - _currentTrackIndex;
         }
      }

      public ITrackInfo currentTrack
      { 
         get
         {
            if (_playQueue.Count <= _currentTrackIndex
                || _currentTrackIndex < 0)
            {
               throw new ApplicationException( "Not playing, no current track" );
            }

            return (ITrackInfo)_playQueue[_currentTrackIndex];
         }
      }

      public ITrackInfo this [int index]
      { 
         get
         {
            if (_playQueue.Count <= index)
               return null;

            return (ITrackInfo)_playQueue[index];
         }
      }

      public int Count
      { 
         get
         {
            return _playQueue.Count;
         }
      }

   }
}
