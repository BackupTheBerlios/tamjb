/// \file
/// $Id$
///

namespace tam.GtkPlayer
{
   using System;
   using System.Collections;
   using System.Data;
   using System.Diagnostics;
   using System.Threading;
   using Gtk;
   using GtkSharp;

   // Need to include things that would  normally be hidden to get
   // the data structs they use. Should these be moved into the global
   // namespace to make them available at the "tam" namespace scope?
   
   public class ConfigDlg : Gtk.Dialog
   {
      ///
      /// \param parent Parent window
      /// \param engine backend object
      /// \param settings is directly modified by ConfigDlg when OK is
      ///   pressed
      ///
      public ConfigDlg( Window parent, 
                        IEngine engine,
                        PlayerSettings settings )
         : base( "TAM Config", parent, DialogFlags.DestroyWithParent )
      {
         _backend = engine;
         _settings = settings;

         Response += new ResponseHandler( _OnResponse );

         AddButton( "Ok", (int)ResponseType.Ok );
         AddButton( "Cancel", (int)ResponseType.Cancel );

         // Now for the state editing controls:
         VBox.Add( new Label( "Suck" ) );

         _serverName = new Entry();
         _serverName.ActivatesDefault = true;
         _serverName.Text = _settings.serverName;
         VBox.Add( _serverName );

         _serverPort = new Entry();
         _serverPort.ActivatesDefault = true;
         _serverPort.Text = _settings.serverPort.ToString();
         VBox.Add( _serverPort );

         VBox.ShowAll();
      }

      void _OnResponse( object o, ResponseArgs args )
      {
         switch (args.ResponseId)
         {
         case (int)ResponseType.Ok:         // save
            Trace.WriteLine( "ConfigDlg: Ok" );
            bool convertOk = false;
            int port = 0;
            try
            {
               port = Convert.ToInt32( _serverPort.Text );
               convertOk = true;
            }
            catch (Exception e)
            {
               // I think we should prevent all invalid entry rather
               // than catching it here after the fact.
               Trace.WriteLine( "Invalid port - " + e.ToString() );
            }
            
            if (convertOk)
            {
               _settings.serverName = _serverName.Text;
               _settings.serverPort = port;
               Hide();
            }

            break;
            
         case (int)ResponseType.Cancel:
            // Restore previous values to controls
            _serverName.Text = _settings.serverName;
            _serverPort.Text = _settings.serverPort.ToString();
            Hide();
            break;
            
         default:
            Trace.WriteLine( "ConfigDlg: Unexpected Response" );
            break;
         }
      }

      IEngine        _backend;
      PlayerSettings _settings;

      Entry          _serverName;
      Entry          _serverPort;
   }
}
