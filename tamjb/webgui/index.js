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
dojo.require("dojox.grid.DataGrid");
dojo.require("dojo.data.ItemFileReadStore");
dojo.require("dojox.data.QueryReadStore");

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

/* Size for future/past display */
var g_historyLength = 5;

// Variables to handle background refresh
var g_refreshAgain = false;

function index_init() 
{
   dojo.connect( jsPastPane, 'onClick', null, '_updatePast' );
   dojo.connect( jsFuturePane, 'onClick', null, '_updateFuture' );

   refresh();
}

dojo.addOnLoad( index_init );

// Initializes or refreshes the mood grid data
function refreshMoodGrid()
{
   jsMoodStore = new dojox.data.QueryReadStore(
      { url : jsMoodStore.url,
        requestMethod : jsMoodStore.requestMethod } );

   jsMoodGrid.setStore( jsMoodStore );
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
      jsLogin.attr('label',"Log In");
      jsMoodBtn.attr('label',"unknown");
      jsMoodBtn.attr("disabled",true);
   }
   else
   {
      jsLogin.attr('label', status.userName );
      jsMoodBtn.attr('label', status.moodName );
      jsMoodBtn.attr("disabled",false);
   }

   dojo.byId("trackId").innerHTML = status.nowPlaying.key;
   dojo.byId("playCount").innerHTML = status.nowPlaying.playCount;
   dojo.byId("title").innerHTML = status.nowPlaying.title;  
   dojo.byId("artist").innerHTML = status.nowPlaying.artist;
   dojo.byId("album").innerHTML = status.nowPlaying.album;
   dojo.byId("filename").innerHTML = status.nowPlaying.filePath;
   dojo.byId("suckLevel").innerHTML = status.suckPercent;
   dojo.byId("moodLevel").innerHTML = status.moodPercent;

   _updatePast();
   _updateFuture();
}

function _updatePast()
{
   if (true != jsPastPane.attr('open')) 
      return;

   // History is an array of HistoryInfo objects. 
   // Let's just put the last few events on the main page... we can put the
   // full array on a history popup or something:

   var history = tjb.getHistory( 'past', g_currentTrackStatus.moodID );
   if (history.length > g_historyLength)
   {
      var first = history.length - g_historyLength;
      history = history.slice( first );
   }

   var data = { identifier: 'key',
                items: history };

   var store = new dojo.data.ItemFileReadStore( { data: data } );

   jsPast.setStore( store );
}

function _updateFuture() {
   if (true != jsFuturePane.attr('open')) 
      return;

   // History is an array of HistoryInfo objects. 
   // Let's just put the last few events on the main page... we can put the
   // full array on a history popup or something:

   var history = tjb.getHistory( 'future', g_currentTrackStatus.moodID );
   if (history.length > g_historyLength)
   {
      history = history.slice( 0, g_historyLength );
   }

   var data = { identifier: 'key',
                items: history };

   var store = new dojo.data.ItemFileReadStore( { data: data } );

   jsFuture.setStore( store );
}

/* Marks current state unknown, and so on */
function startUpdate(msg)
{
   updateStatus=msg;
   jsProgressBar.update({indeterminate: true});

   jsSuckBtn.attr("disabled",true);
   jsMegaSuckBtn.attr("disabled",true);
   jsRuleBtn.attr("disabled",true);
   jsYesBtn.attr("disabled",true);
   jsNoBtn.attr("disabled",true);
}

function finishUpdate()
{
   // Enable controls here?
   updateStatus="OK";
   jsProgressBar.update({indeterminate: false});

   jsSuckBtn.attr("disabled",false);
   jsMegaSuckBtn.attr("disabled",false);
   jsRuleBtn.attr("disabled",false);
   jsYesBtn.attr("disabled",false);
   jsNoBtn.attr("disabled",false);
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
   _suckLess( g_currentTrackStatus.nowPlaying.key );
}

function _suckLess(trackId) {
   try
   {
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

function onSuck() {
   _suckMore( g_currentTrackStatus.nowPlaying.key );
}

function _suckMore( trackId ) {
   try 
   {
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
   _moodYes( g_currentTrackStatus.nowPlaying.key );
}

function _moodYes( trackId )
{
   try
   {
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
   _moodLess( g_currentTrackStatus.nowPlaying.key );
}

function _moodLess(trackId)
{
   try
   {
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
   var row = jsMoodGrid.selection.getFirstSelected();
   if (null == row || row < 0)
      return;

   var moodID = jsMoodGrid.store.getValue( row, "id" );
   _setMood( moodID );

   return true;
}

function onMoodSelectClick()
{
   var row = jsMoodGrid.selection.getFirstSelected();
   if (null == row || row < 0)
      return;

   var moodID = jsMoodGrid.store.getValue( row, "id" );
   _setMood( moodID );
}

function showMoodDialog()
{
   // Always called from the modal mood dialog. Hide that
   jsMoodPopup.hide();
   dijit.byId("moodNameBox").attr('value',"");
   jsMoodCreatePopup.show();
}

function onMoodCreateClick()
{
   // Create mood here.
   var moodName = dijit.byId("moodNameBox").attr('value');
   var moodId = tjb.createMood( moodName );

   jsMoodCreatePopup.hide();

   refreshMoodGrid();             // mood list changed.

   // select the newly created mood.
   _setMood( moodId );
}

function onDelMoodClick()
{
   var row = jsMoodGrid.selection.getFirstSelected();
   if (null == row || row < 0)
      return;

   var moodID = jsMoodGrid.store.getValue( row, "id" );
   var moodName = jsMoodGrid.store.getValue( row, "name" );

   startUpdate( "Deleting mood" );

   // TODO: prompt with dialog here?
   tjb.deleteMood( moodID );
   refreshMoodGrid();
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
         refreshMoodGrid();          // refresh mood list 
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
   var id = dijit.byId("idBox").attr('value');
   var pass = dijit.byId("passwordBox").attr('value');

   dijit.byId("passwordBox").attr('value',"");

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

      refreshMoodGrid();          // refresh mood for current user!
   }
   finally
   {
      finishUpdate();
   }
}

function moodFormatter(moodIn)
{
   return moodIn;
}

function futureMoodGet(rowIndex,item)
{
   return moodGet(rowIndex,item,jsFuture);
}
function pastMoodGet(rowIndex,item)
{
   return moodGet(rowIndex,item,jsPast);
}

function moodGet(rowIndex,item,grid)
{
   if (!item)
      return this.defaultValue;

   var key = grid.store.getValue(item, 'key');
   var mood = grid.store.getValue(item, 'mood');

   var output = '<a href="#' + key + '" onClick="_moodLess(' + key + ')">(-)</a> ';
   output += mood;
   output += '% <a href="#" onClick="_moodMore(' + key + ')">(+)</a>';
   return output;
}

function futureSuckGet(rowIndex,item)
{
   return suckGet(rowIndex,item,jsFuture);
}
function pastSuckGet(rowIndex,item)
{
   return suckGet(rowIndex,item,jsPast);
}

function suckGet(rowIndex,item,grid)
{
   if (!item)
      return this.defaultValue;

   var key = grid.store.getValue(item, 'key');
   var suck = grid.store.getValue(item, 'suck');

   var output = '<a href="#' + key + '" onClick="_suckLess(' + key + ')">(-)</a> ';
   output += suck;
   output += '% <a href="#" onClick="_suckMore(' + key + ')">(+)</a>';
   return output;
}

function probabilityFormatter(prob)
{
   if (prob == 'probHigh')
      return "High";

   if (prob == 'probMedHigh')
      return "MedHi";

   if (prob == 'probMed')
      return "Med";

   if (prob == 'probMedLow')
      return "MedLo";

   if (prob == 'probLow')
      return "Low";

   return "?: " + prob;
}
