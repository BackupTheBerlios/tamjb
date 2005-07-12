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
         // Milliseconds
         _attackScale.Value = _backend.compressAttack * 1000.0;

//          _decayScale.Value = 12 - Math.Log( _backend.compressDecay, 
//                                             DECAY_BASE );
         _decayScale.Value = _backend.compressDecay;

         _bassLevelScale.Value = _backend.compressThresholdBass;
         _midLevelScale.Value = _backend.compressThresholdMid;
         _trebleLevelScale.Value = _backend.compressThresholdTreble;

         _learnButton.Active = _backend.learnLevels;

         _ratioScale.Value = _backend.compressRatio;
         _gateThresholdScale.Value = _backend.gateThreshold;

         // Convert from samples to seconds. Note that this assumes
         // 44.1 samples per second. Approximately. :)
         _predelayScale.Value = 
            ((double)_backend.compressPredelay) / 44.0;
      }

      protected void _OnClose( object sender, EventArgs args )
      {
         _Trace( "[_OnClose]" );
         _miscSettingsDialog.Destroy();
      }

      protected void _OnUserResponse( object sender, ResponseArgs args )
      {
         _Trace( "[_OnUserResponse]" );

         // Well, there's only just the Close button...
         _miscSettingsDialog.Destroy();
      }

      protected void _OnFormatAttack( object sender, FormatValueArgs args )
      {
         try
         {
            args.RetVal = args.Value.ToString( "F1" ) + "ms";
         }
         catch (Exception e)
         {
            _Trace( e.ToString() ); 
         }
      }

      protected void _OnAttackChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnAttackChanged]" );

            // Convert to seconds, and save
            double newVal = _attackScale.Value / 1000.0;

            if (_ChangeRatio(_backend.compressAttack, newVal) > 0.01)
            {
               _Trace( "compressAttack: " + _backend.compressAttack
                       + " --> " + newVal );

               _backend.compressAttack = newVal;
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      protected void _OnDecayChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnDecayChanged]" );

//             // Decay ranges from 0.0000005 to something less than 1.0, 
//             // where 1.0 is a infinitely fast release. I really don't
//             // want an instantaneous release ever! :)
//             // range from 1-10 for this to work:
//             double newVal = Math.Pow( DECAY_BASE, (12 - _decayScale.Value) );

//             if (_ChangeRatio(_backend.compressDecay, newVal) > 0.0001)
//             {
//                _Trace( "compressDecay: " + _backend.compressDecay
//                        + " --> " + newVal );

//                _backend.compressDecay = newVal;
//             }
            _backend.compressDecay = _decayScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      protected void _OnBassLevelChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnBassLevelChanged]" );
            if (_backend.compressThresholdBass != (int)_bassLevelScale.Value)
               _backend.compressThresholdBass = (int)_bassLevelScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      protected void _OnMidLevelChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnMidLevelChanged]" );
            if (_backend.compressThresholdMid != (int)_midLevelScale.Value)
               _backend.compressThresholdMid = (int)_midLevelScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      protected void _OnTrebleLevelChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnTrebleLevelChanged]" );
            if (_backend.compressThresholdTreble != (int)_trebleLevelScale.Value)
               _backend.compressThresholdTreble = (int)_trebleLevelScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      protected void _OnLearnButtonToggled( object sender, EventArgs args )
      {
         try
         {
            _backend.learnLevels = _learnButton.Active;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      protected void _OnRatioChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnRatioChanged]" );

            if (Math.Abs(_backend.compressRatio - _ratioScale.Value) > 0.001)
            {
               _Trace( "compressRatio: " + _backend.compressRatio
                       + " --> " + _ratioScale.Value );
               _backend.compressRatio = _ratioScale.Value;
            }
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      protected void _OnGateChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnGateChanged]" );
            
            if ((int)_gateThresholdScale.Value !=  _backend.gateThreshold)
               _backend.gateThreshold = (int)_gateThresholdScale.Value;
         }
         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      protected void _OnPredelayChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnPredelayChanged]" );

            // Predelay is when the compressor begins to compensate for
            // a change in levels _before_ the sound happens. Predelay
            // is also not the proper name for this.

            // The value is displayed in milliseconds. So...
            uint delayInSamples = (uint)(_predelayScale.Value * 44.0);

            // Limit the range
            if (delayInSamples < 0)
               delayInSamples = 0;

            if (delayInSamples > _backend.compressPredelayMax)
               delayInSamples = _backend.compressPredelayMax;

            if (_backend.compressPredelay != delayInSamples)
               _backend.compressPredelay = delayInSamples;
         }

         catch (Exception e)
         {
            _Trace( e.ToString() );
         }
      }

      ///
      /// Compute the percent change from old to new, using old as the
      /// basis. Well, percent where 100% = 1.0
      ///
      /// This is NOT suitable for values that actually go to 0.0, because
      /// it divides by old. :)
      ///
      double _ChangeRatio( double old, double newVal )
      {
         if (Math.Abs(old) <= 0.0000000001)
            throw new ApplicationException( "overflow problem" );

         return Math.Abs( (old - newVal) / old );
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
      Scale  _bassLevelScale;

      [Glade.Widget]
      Scale  _midLevelScale;

      [Glade.Widget]
      Scale  _trebleLevelScale;

      [Glade.Widget]
      CheckButton  _learnButton;

      [Glade.Widget]
      Scale  _gateThresholdScale; 

      [Glade.Widget]
      Scale  _predelayScale;

   }
}
