/// \file
/// $Id$
///
/// Mood dialog
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
   /// A class to be connected to the Glade main window
   ///
   public class MoodDialog
   {
      public enum DefaultField
      {
         USER,
         MOOD
      }

      public bool isOk
      {
         get
         {
            return _isOk;
         }
      }

      ///
      /// Retrieve user name entered (on OK)
      public string userName 
      {
         get
         {
            return _userName;
         }
      }

      public string moodName
      {
         get
         {
            return _moodName;
         }
      }

      ///
      /// Creates the mood window dialog but does not display it. 
      /// (use Run()).
      ///
      /// cred and mood are directly modified on successful validation.
      ///
      public MoodDialog( Gtk.Window        parent,
                         IEngine           backend,
                         Credentials      cred,
                         Mood             mood,
                         DefaultField      defaultField )
      {
         Debug.Assert( null != backend );

         _backend = backend;    // Save for later...

         Glade.XML glade = new Glade.XML( null,
                                          "tam.GtkPlayer.exe.glade",
                                          "_moodDialog",
                                          null );

         glade.Autoconnect( this );
         
         Debug.Assert( null != _moodDialog );
         Debug.Assert( null != _userCombo );
         Debug.Assert( null != _moodCombo );

         _moodDialog.TransientFor = parent;
         

         // Set up the available users list.

         Credentials [] credList = backend.GetUserList();
         string [] credNameList = new string[ credList.Length ];
         for (int i = 0; i < credList.Length; i++)
            credNameList[i] = ((Credentials)credList[i]).name;

         _userCombo.PopdownStrings = credNameList;


         // Set default values for the entry fields AFTER the dropdown
         // lists are initialized, otherwise it will default to the first
         // value in the list

         if (null != cred)
            _userCombo.Entry.Text = cred.name;

         _UpdateUserBtns();

         if (null != mood)
            _moodCombo.Entry.Text = mood.name;

         _UpdateMoodBtns();

         switch (defaultField)
         {
         case DefaultField.USER:
         default:
            _userCombo.Entry.GrabFocus();
            break;

         case DefaultField.MOOD:
            _moodCombo.Entry.GrabFocus();
            break;
         }
      }

      public void Run()
      {
         _moodDialog.Run();
      }

      ///
      /// Update the options in the mood list pulldown
      void _SetMoodList( Credentials cred )
      {
         Mood [] moodList = _backend.GetMoodList( cred );
         string [] moodNameList = new string[ moodList.Length ];
         for (int i = 0; i < moodList.Length; i++)
            moodNameList[i] = ((Mood)moodList[i]).name;

         _moodCombo.PopdownStrings = moodNameList;

         // Update mood text to match what's in da list?
      }

      ///
      /// Dialog finished response callback
      ///
      void _OnUserResponse( object sender, ResponseArgs args )
      {
         _Trace( "[_OnUserResponse]" );

         switch (args.ResponseId)
         {
         case ResponseType.Ok:
            try
            {
               _userName = _userCombo.Entry.Text;
               _moodName = _moodCombo.Entry.Text;

               // Do some simple validation: must not be empty
               if ("" == _userName)
               {
                  _userCombo.Entry.GrabFocus();
                  return;
               }
            
               _moodDialog.Destroy();
               _isOk = true;
            }
            catch (Exception exception)
            {
               _Trace( exception.ToString() );
            }
            break;
            
         case ResponseType.Cancel:
         default:
            _moodDialog.Destroy();
            break;              // er
         }
      }

      void _OnUserDelete( object sender, EventArgs args )
      {
         MessageDialog md = 
            new MessageDialog( null, 
                               DialogFlags.Modal,
                               MessageType.Error,
                               ButtonsType.Close, 
                               "Delete User - not implemented" );
         
         int result = md.Run ();
      }

      void _OnMoodDelete( object sender, EventArgs args )
      {
         MessageDialog md = 
            new MessageDialog( null, 
                               DialogFlags.Modal,
                               MessageType.Error,
                               ButtonsType.Close, 
                               "Delete Mood - not implemented" );
         
         int result = md.Run ();
      }

      void _OnUserCopy( object sender, EventArgs args )
      {
         MessageDialog md = 
            new MessageDialog( null, 
                               DialogFlags.Modal,
                               MessageType.Error,
                               ButtonsType.Close, 
                               "Copy User - not implemented" );
         
         int result = md.Run ();
      }

      void _OnMoodCopy( object sender, EventArgs args )
      {
         MessageDialog md = 
            new MessageDialog( null, 
                               DialogFlags.Modal,
                               MessageType.Error,
                               ButtonsType.Close, 
                               "Copy Mood - not implemented" );
         
         int result = md.Run ();
      }

      ///
      /// Called when a new user name is selected or entered
      ///
      void _OnUserChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnUserChanged]" );

            _UpdateUserBtns();
         }
         catch (Exception exception)
         {
            _Trace( exception.ToString() );
         }
      }

      void _OnMoodChanged( object sender, EventArgs args )
      {
         try
         {
            _Trace( "[_OnMoodChanged]" );
            _UpdateMoodBtns();
         }
         catch (Exception exception)
         {
            _Trace( exception.ToString() );
         }
      }

      void _UpdateUserBtns()
      {
         // Show only the valid moods for this user!
         string user = _userCombo.Entry.Text;
         Credentials cred = _backend.GetUser( user );
         if (null == cred)
         {
            _userDelBtn.Sensitive = false;
            _userCopyBtn.Sensitive = false;
         }
         else
         {
            _userDelBtn.Sensitive = true;
            _userCopyBtn.Sensitive = true;
            _SetMoodList( cred );
         }
      }

      void _UpdateMoodBtns()
      {
         // Activate/deactivate delete buttons if necessary
         
         string userName = _userCombo.Entry.Text;
         string moodName = _moodCombo.Entry.Text;
         
         if ("" != moodName)
         {
            Credentials cred = _backend.GetUser( userName );
            if (null != cred)
            {
               Mood mood = _backend.GetMood( cred, moodName );
               if (null != mood)
               {
                  _moodDelBtn.Sensitive = true;
                  _moodCopyBtn.Sensitive = true;
                  return;       // ** Mood exists, return **
               }
            }
         }
         
         _moodDelBtn.Sensitive = false;
         _moodCopyBtn.Sensitive = false;
      }


      void _Trace( string msg )
      {
         Trace.WriteLine( msg, "MoodWindow" );
      }

      IEngine _backend;

      string _userName = "";
      string _moodName = "";

      bool _isOk = false;

      [Glade.Widget]
      Dialog _moodDialog;
      
      [Glade.Widget]
      Combo  _userCombo;
      
      [Glade.Widget]
      Combo  _moodCombo;

      [Glade.Widget]
      Button _userDelBtn;

      [Glade.Widget]
      Button _moodDelBtn;

      [Glade.Widget]
      Button _userCopyBtn;

      [Glade.Widget]
      Button _moodCopyBtn;
   }
}
