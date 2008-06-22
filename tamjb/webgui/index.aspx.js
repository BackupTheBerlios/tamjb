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
dojo.require("dijit.ProgressBar");
dojo.require("dijit.TitlePane");
dojo.require("dijit.Toolbar");
dojo.require("dijit.layout.BorderContainer");
dojo.require("dijit.layout.ContentPane");
dojo.require("dijit.layout.SplitContainer");

var tjb = new TJBFunctions();

// Status for status bar
var updateStatus = "idle";
var g_currentTrackStatus;

function index_init() {
  forceRefresh();
}

dojo.addOnLoad( index_init );

function forceRefresh() {
  startUpdate("Refreshing all");
  tjb.getStatus( statusCallback );
}

function statusCallback( response ) {
  if (response.error)
  {
    alertErrorResponse(response);
    return;
  }
  
  updateNowPlaying(response.result);
  finishUpdate();
}

function alertErrorResponse(response) {
  var exceptionType = "unknown";
  if (response.error.errors.length > 0) {
    exceptionType = response.error.errors[0].name;
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
   dijit.byId("moodBtn").setLabel( status.moodName );
}

/* Marks current state unknown, and so on */
function startUpdate(msg)
{
   updateStatus=msg;
   jsProgressBar.update({indeterminate: true});

   dojo.byId("trackId").innerHTML = "-";
   dojo.byId("title").innerHTML = "-";
   dojo.byId("artist").innerHTML = "-";
   dojo.byId("album").innerHTML = "-";
   dojo.byId("filename").innerHTML = "-";
   dojo.byId("suckLevel").innerHTML = "-";
   dojo.byId("moodLevel").innerHTML = "-";
   dijit.byId("moodBtn").setLabel( "(unknown mood)" );
}

function finishUpdate()
{
   // Enable controls here?
   updateStatus="OK";
   jsProgressBar.update({indeterminate: false});
}

// Helper for the progress bar
function progressReport(percent) {
   // TODO: if updating history lists, say so.
   return updateStatus;
}

// Callback handler for rule/suck/etc controls
function onTransportCtrlFinished(response) {
   if (response.error) {
      alertErrorResponse(response);
      return;
   }

   updateNowPlaying(response.result);
   finishUpdate();
}

function onMegaSuck() {
}

function onRule() {
   try
   {
      var trackId = g_currentTrackStatus.nowPlaying.key;
      var title = dojo.byId("title").innerHTML;
      if (-1 == trackId) {
         alert("Track ID is -1, not valid. Hmm.");
         return;
      }
      
      startUpdate("Decreasing suck for (" + trackId + "): "
                  + title);
      
      tjb.suckLess( trackId, onTransportCtrlFinished );
   }
   catch(err)
   {
      alert( err );
      finishUpdate();
   }
}

function onSuck() {
   try 
   {
      var trackId = g_currentTrackStatus.nowPlaying.key;
      if (!trackId || (-1 == trackId)) {
         alert("Track ID is -1, not valid. Hmm.");
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

function onMood() {
   try 
   {
      alert( "TODO" );
   }
   catch(err)
   {
      alert( err );
   }
}
