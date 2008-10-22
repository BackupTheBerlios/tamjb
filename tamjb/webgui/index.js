// Copyright (C) 2008 Tom Surace.
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

// "Request" some dojo bits
dojo.require("dojo.parser");
dojo.require("dijit.form.Button");
dojo.require("dijit.form.CheckBox");
dojo.require("dijit.form.ValidationTextBox");
dojo.require("dijit.form.TextBox");
dojo.require("dijit.Dialog");
dojo.require("dijit.ProgressBar");
dojo.require("dijit.TitlePane");
dojo.require("dijit.Toolbar");
dojo.require("dijit.layout.BorderContainer");
dojo.require("dijit.layout.ContentPane");
dojo.require("dijit.layout.SplitContainer");
dojo.require("dojo.data.ItemFileReadStore");
dojo.require("dojox.grid.Grid");
dojo.require("dojox.grid._data.model");
dojo.require("dojox.data.QueryReadStore");
dojo.require("dojo.data.ItemFileReadStore");

var tjb = new TJBFunctions();

// Status for status bar
var updateStatus = "idle";
// Dummy structure for first login
var g_currentTrackStatus = { 
   moodID: -1,
   changeCount: -1,
   nowPlaying: {
      key: -1
   }
};

var g_timer;
var g_moodDlg;

// Variables to handle background refresh
var g_refreshAgain = false;

function index_init() {

   _initMoodGrid();
  jsMoodGrid.selection.multiSelect = false;

  refresh();
}

dojo.addOnLoad( index_init );
// dojo.addOnLoad();

// Initializes or refreshes the mood grid data
function _initMoodGrid()
{
  var moodView1 = {
     cells: [
        [{name: 'Mood', field: 1, width: "25em"},
         {name: 'ID', field: 0}]
        ]
  };
  var moodLayout = [ moodView1 ];

  var store = 
     new dojox.data.QueryReadStore({url: 'moodstore.ashx', 
                                   identifier: 'id',
                                   requestMethod: 'post',
                                   });
  var moodModel =
     new dojox.grid.data.DojoData(null,null,
                                  {store: store, 
                                   clientSort: false} );

  // Now set the model and structure
  jsMoodGrid.setModel( moodModel );
  jsMoodGrid.setStructure( moodLayout );
}


function refresh(force) {
   if (force)
      g_currentTrackStatus.changeCount = -1;

   // really this is unlikely to fail, but we don't want to lose the
   // background refresh ever!
   try
   {
      // Don't want refresh to happen while we're refreshing.
      if (g_timer) clearTimeout(g_timer);

      startUpdate("Refreshing all");
      var status = tjb.getStatus( g_currentTrackStatus.changeCount );
      updateNowPlaying( status );
   }
   catch (response_error)
   {
      var exceptionType;
      if (response_error && response_error.errors 
          && (response_error.errors.length > 0)) 
      {
         exceptionType = response_error.errors[0].name;
      }
  
      var message = response_error.message;
      if ("ApplicationException" == exceptionType && "login" == message) 
      {
         // Redirect to login page the ugly way, for now.
         jsLoginPopup.show();
         return;
      }
   }
   finally
   {
      // Don't give up!
      g_timer = setTimeout("refresh()",15000);

      finishUpdate();
   }
}

// function refreshCallback( response ) {
//    try
//    {
//       if (response.error)
//       {
//          alertErrorResponse(response);
//          return;
//       }

//       if (response.result)
//          updateNowPlaying(response.result);
//    }
//    finally
//    {
//       g_waitingForRefresh = false;
//       g_timer = setTimeout("refresh()",15000);
//       finishUpdate();
//    }

//    // Did state change while we were refreshing? *sigh*
//    if (g_refreshAgain) {
//       g_refreshAgain = false;
//       refresh();
//    }
// }

function alertErrorResponse(response)
{
   if (undefined == response)
   {
      // What to do?
      alert("Null response from server?");
      return;
   }

   var exceptionType = "unknown";
   if (response.name) {
      exceptionType = response.name;
   }

   if (response.error.errors.length > 0) {
      exceptionType = response.error.errors[0].name;
   }
  
   var message = response.error.message;
   if ("ApplicationException" == exceptionType && "login" == message) 
   {
      // Redirect to login page the ugly way, for now.
      jsLoginPopup.show();
      return;
   }

   alert("Unhandled exception (" +
         exceptionType + 
         "): " + 
         response.error.message);
}

/* status is byteheaven.tamjb.webgui.StatusInfo */
function updateNowPlaying(status)
{
   // If this is false, there is no data in the status object.
   if (false == status.statusChanged)
      return;

   g_currentTrackStatus = status;

   if ("" == status.userName)
   {
      jsLogin.setLabel("Log In");
      jsMoodBtn.setLabel("unknown");
      jsMoodBtn.setAttribute("disabled",true);
   }
   else
   {
      jsLogin.setLabel( status.userName );
      jsMoodBtn.setLabel( status.moodName );
      jsMoodBtn.setAttribute("disabled",false);
   }

   dojo.byId("trackId").innerHTML = status.nowPlaying.key;
   dojo.byId("playCount").innerHTML = status.nowPlaying.playCount;
   dojo.byId("title").innerHTML = status.nowPlaying.title;  
   dojo.byId("artist").innerHTML = status.nowPlaying.artist;
   dojo.byId("album").innerHTML = status.nowPlaying.album;
   dojo.byId("filename").innerHTML = status.nowPlaying.filePath;
   dojo.byId("suckLevel").innerHTML = status.suckPercent;
   dojo.byId("moodLevel").innerHTML = status.moodPercent;

}

/* Marks current state unknown, and so on */
function startUpdate(msg)
{
   updateStatus=msg;
   jsProgressBar.update({indeterminate: true});

   jsSuckBtn.setAttribute("disabled",true);
   jsMegaSuckBtn.setAttribute("disabled",true);
   jsRuleBtn.setAttribute("disabled",true);
   jsYesBtn.setAttribute("disabled",true);
   jsNoBtn.setAttribute("disabled",true);
}

function finishUpdate()
{
   // Enable controls here?
   updateStatus="OK";
   jsProgressBar.update({indeterminate: false});

   jsSuckBtn.setAttribute("disabled",false);
   jsMegaSuckBtn.setAttribute("disabled",false);
   jsRuleBtn.setAttribute("disabled",false);
   jsYesBtn.setAttribute("disabled",false);
   jsNoBtn.setAttribute("disabled",false);
}

// Helper for the progress bar
function progressReport(percent) {
   // TODO: if updating history lists, say so.
   return updateStatus;
}

// Callback handler for rule/suck/etc controls
function onTransportCtrlFinished(response) {
   try
   {
      if (response.error) {
         alertErrorResponse(response);
         return;
      }

      if (response.result)
      {
         updateNowPlaying(response.result);
      }
   }
   finally
   {
      finishUpdate();
   }
}

function onRule() {
   try
   {
      var trackId = g_currentTrackStatus.nowPlaying.key;
      if (-1 == trackId) {
         alert("Track ID is -1, not valid. Hmm.");
         return;
      }
      
      startUpdate("Decreasing suck for (" + trackId + "): "
                  + g_currentTrackStatus.title);
      
      tjb.suckLess( trackId, onTransportCtrlFinished );
   }
   catch(err)
   {
      alert(err);
      finishUpdate();
   }
}

function onSuck()
{
   try 
   {
      var trackId = g_currentTrackStatus.nowPlaying.key;
      if (-1 == trackId) {
         alert("Track ID is not valid. Hmm.");
         return;
      }

      startUpdate("Increasing suck for (" + trackId + "): "
                  + g_currentTrackStatus.title);

      tjb.suckMore( trackId, onTransportCtrlFinished );
   }
   catch(err)
   {
      alert( err );
      finishUpdate();
   }
}

function onMegaSuck() {
   try 
   {
      var trackId = g_currentTrackStatus.nowPlaying.key;
      if (-1 == trackId) {
         alert("Track ID is -1, not valid. Hmm.");
         return;
      }

      startUpdate("Mega Suck! (" + trackId + "): "
                  + g_currentTrackStatus.title);

      tjb.megaSuck( trackId, onTransportCtrlFinished );
   }
   catch(err)
   {
      alert( err );
      finishUpdate();
   }
}

function onYes() {
   try
   {
      var trackId = g_currentTrackStatus.nowPlaying.key;
      if (-1 == trackId) {
         alert("Track ID is not valid. Hmm.");
         return;
      }
      
      var moodId = g_currentTrackStatus.moodID;
      if (undefined == moodId || -1 == moodId) {
         alert("Mood ID is not valid.");
         return;
      }

      startUpdate("Increasing mood for (" + trackId + "): "
                  + g_currentTrackStatus.title);
      
      tjb.moodYes( trackId, moodId, onTransportCtrlFinished );
   }
   catch(err)
   {
      alert(err);
      finishUpdate();
   }
}

function onNo() {
   try
   {
      var trackId = g_currentTrackStatus.nowPlaying.key;
      if (-1 == trackId) {
         alert("Track ID is not valid. Hmm.");
         return;
      }
      
      var moodId = g_currentTrackStatus.moodID;
      if (undefined == moodId || -1 == moodId) {
         alert("Mood ID is not valid.");
         return;
      }

      startUpdate("Decreasing mood for (" + trackId + "): "
                  + g_currentTrackStatus.title);
      
      tjb.moodNo( trackId, moodId, onTransportCtrlFinished );
   }
   catch(err)
   {
      alert( err );
      finishUpdate();
   }
}

function onMood() 
{
   // Initialize the selection to the current mood:
   if (g_currentTrackStatus.moodID < 0)
      return;

   // jsMoodGrid.selection.select( row );
   jsMoodPopup.show();
}

function onMoodRowClick(evt)
{
   var row = evt.rowIndex;
   if (row < 0)
      return;

   // offset 0 is id.
   var moodID = jsMoodGrid.model.getDatum( row, 0 );
   _setMood( moodID );

   return true;
}

function onMoodSelectClick()
{
   var row = jsMoodGrid.selection.getFirstSelected();
   if (row < 0)
      return;

   var moodID = jsMoodGrid.model.getDatum( row, 0 );
   _setMood( moodID );
}

function showMoodDialog()
{
   // Always called from the modal mood dialog. Hide that
   jsMoodPopup.hide();
   dijit.byId("moodNameBox").setValue("");
   jsMoodCreatePopup.show();
}

function onMoodCreateClick()
{
   // Create mood here.
   var moodName = dijit.byId("moodNameBox").getValue();
   var moodId = tjb.createMood( moodName );

   jsMoodCreatePopup.hide();

   _initMoodGrid();             // mood list changed.

   // select the newly created mood.
   _setMood( moodId );
}

function onDelMoodClick()
{
   var row = jsMoodGrid.selection.getFirstSelected();
   if (row < 0)
      return;

   var moodID = jsMoodGrid.model.getDatum( row, 0 );
   var moodName = jsMoodGrid.model.getDatum( row, 1 );

   startUpdate( "Deleting mood" );

   // TODO: prompt with dialog here?
   tjb.deleteMood( moodID );
   _initMoodGrid();
   finishUpdate();
}

// Helper for add/delete mood
function _onMoodModifyFinished()
{
   try
   {
      if (response.error) {
         alertErrorResponse(response);
         return;
      }

      if (response.result)
      {
         _initMoodGrid();          // refresh mood list 
      }
   }
   finally
   {
      finishUpdate();
   }
}

function _refreshMoodList()
{
   
}

function _setMood(moodID)
{
   try
   {
      startUpdate( "Setting mood" );
      tjb.setMood( moodID, onTransportCtrlFinished );
      jsMoodPopup.hide();
   }
   catch(err)
   {
      finishUpdate();
      alert(err);
   }
}

///
/// Toggle login
///
function onLogin()
{
   jsLoginPopup.show();
}

function onLogOut()
{
   tjb.logout( logOutCallback );
}

function logOutCallback( result )
{
   refresh(true);
}

///
/// Called when the login is complete
///
function onIdentify()
{
   dojo.byId("loginError").innerHTML = "";
   startUpdate( "Authenticating" );
   var id = dijit.byId("idBox").getValue();
   var pass = dijit.byId("passwordBox").getValue();

   dijit.byId("passwordBox").setValue("");

   tjb.login( id, pass, onIdentifyCallback );
}


function onIdentifyCallback(response)
{
   try
   {
      // If we got here, we're logged in.
      if (undefined == response
          || response.error)
      {
         alertErrorResponse(response);
         return;
      }

      if (response.result.userName == "")
      {
         dojo.byId("loginError").innerHTML = "Authentication Failure";
         return;
      }

      jsLoginPopup.hide();

      // This will update the logged-in status and everything
      // if we are now logged in. 
      updateNowPlaying(response.result);

      _initMoodGrid();          // refresh mood for current user!
   }
   finally
   {
      finishUpdate();
   }
}
