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
//   Tom Surace <tekhedd@byteheaven.net>


namespace byteheaven.tamjb.SimpleMp3Player
{
   using System;
   using System.IO;
   using System.Diagnostics;

   using System.Runtime.InteropServices; // for Marshal et al

   ///
   /// Streaming output interface for "classic wave audio" on windows. Ugh.
   ///
   public class OutStreamWin32 :
      public IOutStream
   {
      public ~OutStreamWin32()
      {
         Close();
      }

      ///
      /// Opens the audio system, enqueues buffers, and otherwise
      /// gets us ready to play.
      ///
      public void Open()
      {
         lock (_mainLock)
         {
            if (_isOpen)
               throw new ApplicationExcption( "Open while already open" );
            
            WAVEFORMATEX wfx = new WAVEFORMATEX();
            wfx.wFormatTag = WAVE_FORMAT_PCM;
            wfx.nChannels = 2;     // stereo
            wfx.nSamplesPerSec = 44100;
            wfx.nBlockAlign = 4;   // four bytes per frame, see?
            wfx.wBitsPerSample = 16;
            wfx.nAvgBytesPerSec = wfx.nBlockAlign * wfx.nSamplesPerSec;
            wfx.cbSize = 0; // no extra info

            int status = waveOutOpen( out _waveOutHandle,
                                      WAVE_MAPPER, 
                                      wfx, 
                                      _playEvent.SafeWaitHandle,
                                      CALLBACK_EVENT,
                                      0
                                      );
            
            if (status != MMSYSERR_NOERROR) 
            {
               throw new ApplicationException(
                  "unable to open the default WAVE device" );
            }

            _isOpen = true;
            
            // Create audio buffers, including unmanaged data buffers
            for (int i = 0; i < N_WAVE_HEADERS; i++)
            {
               WAVEHDR header = new WAVEHDR();

               IntPtr data = Marshal.AllocHGlobal( size );
               
               header.lpData = data;     // pointer to the data buffer
               header.dwBufferLength = 0; // set on each call
               header.dwBytesRecorded = 0;
               header.dwUser = size; // hide the real buffer size here
               header.dwFlags = WHDR_DONE; // so it'll get enqueued
               header.dwLoops = 0;
               header.lpNext = 0;  // we don't mess with this pointer
               header.reserved;    // or this int

               // Yeah, this sucker's gonna be pinned for the duration.
               // This is probably bad. It is better if we unpin it on
               // each buffer switch? Probably not.
               _waveHdr[i] = GCHandle.Alloc( header, GCHandldType.Pinned );
            }
         }
      }

      public void Close()
      {
         lock (_mainLock)
         {
            if (_isOpen)
            {
               waveOutReset( _waveOutHandle );

               // Wait for the wave system to release buffers?

               for (int i = 0; i < N_WAVE_HEADERS; i++)
               {
                  // This will cause a crash if they are still in use. Ick.
                  WAVEHDR header = (WAVEHDR)_waveHdr[i].Target;
                  if (header.dwFlags & WHDR_PREPARED)
                  {
                     int status = waveOutUnprepareHeader
                        ( _waveOutHandle,
                          _waveHdr[i].ToIntPtr(),
                          Marshal.sizeof(typeof(WAVEHDR)) );
                  
                     if (status != MMSYSERR_NOERROR)
                     {
                        throw new ApplicationException( 
                           String.Format( "Error '{0}' unpreparing wave header on shutdown",
                                          status ) 
                           );
                     }
                  }                     

                  Marshal.FreeHGlobal( header.lpData );
                  _waveHdr[i].Free();
               }

               waveOutClose( _waveOutHandle );
               _isOpen = false;
            }
         }
      }


      ///
      /// Implements IOutStream.Write
      ///
      public int Write( byte[] buffer, int offset, int length )
      {
         lock (_mainLock)
         {
            IntPtr nextIntPtr;
            WAVEHDR header;
            _GetFreeHeader( out nextIntPtr, out header );

            // Fill this buffer as much as possible. If it does not fit
            // in the buffer, throw an exception because, hey, you had
            // a choice of what buffer size when you called Open, it's
            // your fault for exceeding it, you silly person!

            if (length > header.dwUser)
            {
               throw new ApplicationException( 
                  "Length exceeds allocated buffer size on Write" );
            }

            Marshal.Copy( buffer, offset, header.lpData, length );
            header.dwBufferLength = length;

            header.lpData = rawData;
            header.dwBufferLength = length;
            header.dwFlags = 0;
            header.dwLoops = 0;

            int status = waveOutPrepareHeader
               ( _waveOutHandle,
                 nextIntPtr,
                 Marshal.sizeof(typeof(WAVEHDR))) );

            if (status != MMSYSERR_NOERROR)
            {
               throw new ApplicationException( 
                  String.Format( "Error '{0}' preparing wave header",
                                 status ) 
                  );
            }
    
            status = waveOutWrite( _waveOutHandle,
                                   nextIntPtr,
                                   Marshal.sizeof(typeof(WAVEHDR)) );

            if (status != MMSYSERR_NOERROR)
            {
               throw new ApplicationException( 
                  String.Format( "Error '{0}' writing wave header" );
            }
         }
      }

      ///
      /// Returns the next header we should write to, or blocks until
      /// one is available. The returned header is done and prepared.
      ///
      void _GetFreeHeader( out GCHandle nextIntPtr,
                           out WAVEHDR nextAsHeader )
      {
         while (true)
         {
            // Clear the wave event before checking "done" to 
            // reduce the race condition between checking and 
            // waiting on the event. (it's manual reset...)
            _playEvent.Reset();

            // Note that the header memory fields will be updated
            // by the wave engine at the same time. Oooh, scary!
            // OK, so we're just checking one bit in an int, it's not
            // really dangerous.

            nextFree = _waveHdr[_nextFreeHeader];
            nextAsHeader = (WAVEHDR)nextFree.Target;
            nextIntPtr = nextIntPtr.ToIntPtr();

            if (nextAsHeader.dwFlags & WHDR_DONE)
            {
               if (nextAsHeader.dwFlags & WHDR_PREPARED)
               {
                  int status = waveOutUnprepareHeader
                     ( _waveOutHandle,
                       nextIntPtr,
                       Marshal.sizeof(typeof(WAVEHDR)) );
                  
                  if (status != MMSYSERR_NOERROR)
                  {
                     // Ugh? Still playing? But WHDR_DONE is set!
                     throw new ApplicationException( 
                        String.Format( "Error '{0}' unpreparing wave header",
                                       status ) 
                        );
                  }
               }

               // Switch buffers. Whee.
               _nextFreeHeader = _nextFreeHeader ^ 0x01;
               return;
            }

            // The next buffer is not done. Wait for it to be done.
            // Note there is a race condition here. :(
            _playEvent.WaitOne( 500, false );

            // Could check here to avoid non-buffer-done type events,
            // but really it doesn't matter.
         }
      }

      ///
      /// Callback if we are using the callback function notification.
      /// This would be horrilby unsafe with the old 16 bit audio system,
      /// but hey .NET doesn't run under win16 anyway. So there.
      ///
      public delegate void WaveOutProcDelegate( IntPtr waveOutHandle,
                                                ushort uMsg,
                                                IntPtr dwInstance,
                                                uint   dwParam1,
                                                uint   dwParam2 );

      ///
      /// We will synchronize on this object
      ///
      object _mainLock = new object();

      ///
      /// Internal flag so we know whether the wave device is open.
      ///
      bool _isOpen = false;

      IntPtr _waveOutHandle = INVALID_HANDLE_VALUE;

      ///
      /// Front/back buffer info structs, referenced by the 
      /// audio system.
      ///
      GCHandle [] _waveHdr = new GCHandle[2];

      ///
      /// Signalled by the audio system when a buffer is full.
      ///
      ManualResetEvent _playEvent = new ManualResetEvent( false );

      //
      // Some constants for dealing with the wave system.
      //

      // waveOutOpen flags:
      const int CALLBACK_NULL       = 0x00000001;
      const int CALLBACK_FUNCTION   = 0x00030000;
      const int CALLBACK_EVENT      = 0x00050000; 
      
      const short WAVE_FORMAT_PCM = 0x01;
      const int MMSYSERR_NOERROR = 0;

      const int WHDR_DONE           = 0x01;
      const int WHDR_PREPARED       = 0x02;

      const IntPtr INVALID_HANDLE_VALUE = new IntPtr( -1 );

      ///
      /// Struct that maps to the standard wave audio WAVEHDR struct.
      ///
      [StructLayout(LayoutKind.Sequential,Pack=1)] 
      struct WAVEHDR 
      {
         IntPtr lpData;            // pointer to the data buffer
         uint   dwBufferLength;
         uint   dwBytesRecorded;
         uint   dwUser;
         uint   dwFlags;
         uint   dwLoops;
         IntPtr lpNext;            // we don't mess with this pointer
         uint   reserved;          // or this int
      }

      ///
      /// OK, so WORD's are bytes, and DWORDs are uints. Right? 
      ///
      [StructLayout(LayoutKind.Sequential,Pack=1)] 
      struct WAVEFORMATEX
      {
         /* WORD  */ushort wFormatTag;
         /* WORD  */short nChannels;
         /* DWORD */uint nSamplesPerSec;
         /* DWORD */uint nAvgBytesPerSec;
         /* WORD  */ushort nBlockAlign;
         /* WORD  */ushort wBitsPerSample;
         /* WORD  */ushort cbSize;
      }

      
      [DllImport( mmdll )]
      protected static extern
      int waveOutClose( IntPtr waveOutHandle );
      
      [DllImport( mmdll )]
      protected static extern
      int waveOutGetNumDevs();
      
      [DllImport( mmdll )]
      protected static extern
      int waveOutGetVolume( IntPtr waveOutHandle,
                            out int volume );

      [DllImport( mmdll )]
      protected static extern
      int waveOutGetPosition( IntPtr waveOutHandle,
                              out int info,
                              int size );

      [DllImport( mmdll )]
      protected static extern
      int waveOutOpen( out IntPtr waveOutHandle,
                       int deviceId,
                       WAVEFORMATEX format,
                       SafeWaitHandle event_handle,
                       int instanceData,
                       int flags );

      [DllImport( mmdll )]
      protected static extern
      int waveOutPause( IntPtr waveOutHandle );

      [DllImport( mmdll )]
      protected static extern
      int waveOutPrepareHeader( IntPtr waveOutHandle,
                                ref WAVEHDR waveHdr,
                                int size );

      [DllImport( mmdll )]
      protected static extern
      int waveOutReset( IntPtr waveOutHandle );
        
      [DllImport( mmdll )]
      protected static extern
      int waveOutRestart( IntPtr waveOutHandle );

      [DllImport( mmdll )]
      protected static extern
      int waveOutSetVolume( IntPtr waveOutHandle,
                            int volume );

      [DllImport( mmdll )]
      protected static extern
      int waveOutUnprepareHeader( IntPtr waveOutHandle,
                                  ref WAVEHDR waveHeader,
                                  int size );

      [DllImport( mmdll )]
      protected static extern
      int waveOutWrite( IntPtr waveOutHandle,
                        ref WAVEHDR waveHeader,
                        int size );

   }

}
