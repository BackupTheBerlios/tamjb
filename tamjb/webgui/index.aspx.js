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
var g_currentTrackStatus;
var g_timer;
var g_moodDlg;
var g_changeCount = -1;

// Variables to handle background refresh
var g_waitingForRefresh = false;
var g_refreshAgain = false;

function index_init() {
  refresh();
}

dojo.addOnLoad( index_init );
// dojo.addOnLoad();

function refresh(force) {
   if (force)
      g_changeCount = -1;

   // Prevent overlapping refresh calls.
   // Note: assumes javascript is not reentrant. It isn't, right?
   if (g_waitingForRefresh)
   {
      g_refreshAgain = true;
      return;
   }

   // really this is unlikely to fail, but we don't want to lose the
   // background refresh ever!
   try
   {
      if (g_timer) clearTimeout(g_timer);

      startUpdate("Refreshing all");
      g_waitingForRefresh = true;
      tjb.getStatus( g_changeCount, refreshCallback );
   }
   catch(err)
   {
      // Don't give up!
      g_waitingForRefresh = false;
      g_timer = setTimeout("refresh()",30000);

      finishUpdate();
   }
}

function refreshCallback( response ) {
   try
   {
      if (response.error)
      {
         alertErrorResponse(response);
         return;
      }

      // If this is false, there is no data in the status object.
      if (false == response.result.statusChanged)
         return;

      g_changeCount = response.result.changeCount;
      updateNowPlaying(response.result);
   }
   finally
   {
      g_waitingForRefresh = false;
      g_timer = setTimeout("refresh()",15000);
      finishUpdate();
   }

   // Did state change while we were refreshing? *sigh*
   if (g_refreshAgain) {
      g_refreshAgain = false;
      refresh();
   }
}

function alertErrorResponse(response) {
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
     alert("You are not logged in.");
     window.location="login.aspx";
     return;
  }

  alert("Unhandled exception (" +
    exceptionType + 
    "): " + 
    response.error.message);
}

function updateNowPlaying(status)
{
   g_currentTrackStatus = status;

   /* status is byteheaven.tamjb.webgui.StatusInfo */
   dojo.byId("trackId").innerHTML = status.nowPlaying.key;
   dojo.byId("title").innerHTML = status.nowPlaying.title;  
   dojo.byId("artist").innerHTML = status.nowPlaying.artist;
   dojo.byId("album").innerHTML = status.nowPlaying.album;
   dojo.byId("filename").innerHTML = status.nowPlaying.filePath;
   dojo.byId("suckLevel").innerHTML = status.suckPercent;
   dojo.byId("moodLevel").innerHTML = status.moodPercent;

   jsMoodBtn.setLabel( status.moodName );
}

/* Marks current state unknown, and so on */
function startUpdate(msg)
{
   updateStatus=msg;
   jsProgressBar.update({indeterminate: true});

   jsMoodBtn.setAttribute("disabled",true);
   jsSuckBtn.setAttribute("disabled",true);
   jsMegaSuckBtn.setAttribute("disabled",true);
   jsRuleBtn.setAttribute("disabled",true);
}

function finishUpdate()
{
   // Enable controls here?
   updateStatus="OK";
   jsProgressBar.update({indeterminate: false});

   jsMoodBtn.setAttribute("disabled",false);
   jsSuckBtn.setAttribute("disabled",false);
   jsMegaSuckBtn.setAttribute("disabled",false);
   jsRuleBtn.setAttribute("disabled",false);
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

      updateNowPlaying(response.result);
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
      if (!trackId || (-1 == trackId)) {
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
      if (!trackId || (-1 == trackId)) {
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
      if (undefined == trackId || -1 == trackId) {
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


function onMood() {
  jsMoodPopup.show();
}
