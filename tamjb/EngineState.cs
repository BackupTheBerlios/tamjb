/// \file
/// $Id$

namespace tam
{
   using System;
   using System.Collections;

   [Serializable]
   public class EngineState : IEngineState
   {
      bool      _isPlaying;
      int       _currentTrackIndex;
      ArrayList _playQueue; // What's coming up, and what we've played.
      long      _trackCounter;
      long      _changeCount;

      public EngineState( bool isPlaying,
                          int  currentTrackIndex,
                          ArrayList playQueue,
                          long trackCounter,
                          long changeCount )
      {
         _isPlaying = isPlaying;
         _currentTrackIndex = currentTrackIndex;
         _playQueue = playQueue; // should make a copy instead of ref?
         _trackCounter = trackCounter;
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

      /// 
      /// This number indicates the index of the current
      /// playing track, and is passed to gotoNext/Prev/etc
      /// calls. 
      ///
      /// There's probably a better way to do this.
      ///
      public long trackCounter
      { 
         get
         {
            return _trackCounter;
         }
      }
   }
}
