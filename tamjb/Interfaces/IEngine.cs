// $Id$
// Engine base type for remoting

namespace tam
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

      /// 
      /// This number indicates the index of the current
      /// playing track, and is passed to gotoNext/Prev/etc
      /// calls. 
      ///
      /// There's probably a better way to do this.
      ///
      long trackCounter{ get; }
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
      /// This is for the server only, not the client
      ///
      void Poll();

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
      uint GetAttribute( uint playlistKey, uint trackKey );
      void SetAttribute( uint playlistKey, uint trackKey, uint newValue );
      IPlaylistCriterion GetCriterion( uint index );

      // The attribute selection control needs work.
      void ActivateCriterion( uint index );
      void DeactivateCriterion( uint index );

      // These could possibly be changed to indicate the percieved 
      // "current value" of the attribute, so the server can be more
      // intelligent.
      void IncreaseAttributeZenoStyle( uint attributeKey,
                                       uint trackKey );

      void DecreaseAttributeZenoStyle( uint attributeKey,
                                       uint trackKey );


      // These need some sort of cue as to what your application's 
      // state is to deal with multiple concurrent requests.
      void GotoNextFile();
      void GotoPrevFile();

   }
}
