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
//   Tom Surace <tekhedd@byteheaven.net>

namespace byteheaven.tamjb.GtkPlayer
{
   using System;
   using System.Collections;
   using System.Collections.Specialized;
   using System.Diagnostics;
   using System.Runtime.Remoting;
   using System.Runtime.Remoting.Channels;
   using System.Runtime.Remoting.Channels.Http;
   using System.Runtime.Remoting.Channels.Tcp;
   using System.Threading;
   using Gtk;
   using GtkSharp;
   using Glade;

   using byteheaven.tamjb.Interfaces;

   ///
   /// A dialog that sets up miscellaneous things like compression
   /// settings, etc. Directly modifies the backend, no cancel is possible!
   ///
   public class MiscSettingsDialog
   {
      static readonly double ATTACK_BASE = 0.38;
      static readonly double DECAY_BASE = 0.23;

      ///
      /// Creates the mood window dialog but does not display it. 
      /// (use Run()).
      ///
      /// cred and mood are directly modified on successful validation.
      ///
      public MiscSettingsDialog( Gtk.Window        parent,
                                 IEngine           backend )
      {
         _backend = backend;

         Glade.XML glade = new Glade.XML( null,
                                          "tam.GtkPlayer.exe.glade",
                                          "_miscSettingsDialog",
                                          null );

         glade.Autoconnect( this );
         
         Debug.Assert( null != _miscSettingsDialog );

         _miscSettingsDialog.TransientFor = parent;

         _UpdateSettingsFromBackend();
      }

      public void Run()
      {
         _miscSettingsDialog.Run();
      }

      void _UpdateSettingsFromBackend()
      {
         _attackScale.Value = 10 - Math.Log( _backend.compressAttack, 
                                             ATTACK_BASE );

         _decayScale.Value = 12 - Math.Log( _backend.compressDecay, 
                                            DECAY_BASE );

         _targetScale.Value = _backend.compressThreshold;
         _ratioScale.Value = _backend.compressRatio;
         _gateThresholdScale.Value = _backend.gateThreshold;

         _clipThresholdScale.Value = _backend.clipThreshold;
      }

      void _OnClose( object sender, EventArgs args )
      {
         _Trace( "[_OnClose]" );
         _miscSettingsDialog.Destroy();
      }

      void _OnUserResponse( object sender, ResponseArgs args )
      {
         _Trace( "[_OnUserResponse]" );

         // Well, there's only just the Close button...
         _miscSettingsDialog.Destroy();
      }

      void _OnFormatAttack( object sender, FormatValueArgs args )
      {
         try
         {
            _Trace( "[_OnFormatAttack]" );
            args.RetVal = args.Value.ToString( "F1" );
         }
         catch (Exception e)
         {
            _Trace( e.ToString() ); 
         }
      }

      void _OnAttackChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnAttackChanged]" );

            // Attack ranges from 0.0002 to 1.0, where 1.0 is a infinitely fast
            // attack, and 0.0002 is pretty slow. The scale value should
            // range from 0-9 for this to work:
            double newVal = Math.Pow( ATTACK_BASE, (10 - _attackScale.Value) );
            _backend.compressAttack = newVal;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _OnDecayChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnDecayChanged]" );

            // Attack ranges from 0.0000005 to something less than 1.0, 
            // where 1.0 is a infinitely fast release. I really don't
            // want an instantaneous release ever! :)
            // range from 1-10 for this to work:
            double newVal = Math.Pow( DECAY_BASE, (12 - _decayScale.Value) );
            _backend.compressDecay = newVal;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _OnTargetChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnTargetChanged]" );
            _backend.compressThreshold = (int)_targetScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _OnRatioChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnRatioChanged]" );
            _backend.compressRatio = _ratioScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _OnGateChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnGateChanged]" );
            _backend.gateThreshold = (int)_gateThresholdScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _OnClipChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnClipThresholdChanged]" );
            _backend.clipThreshold = (int)_clipThresholdScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "MiscSettingsDialog" );
      }

      IEngine _backend;

      [Glade.Widget]
      Dialog _miscSettingsDialog;

      [Glade.Widget]
      Scale  _attackScale;

      [Glade.Widget]
      Scale  _decayScale;

      [Glade.Widget]
      Scale  _ratioScale;

      [Glade.Widget]
      Scale  _targetScale;

      [Glade.Widget]
      Scale  _gateThresholdScale; 

      [Glade.Widget]
      Scale  _clipThresholdScale;   

   }
}
