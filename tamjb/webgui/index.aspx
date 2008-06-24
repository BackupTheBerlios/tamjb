<%@ Page language="C#" 
  EnableViewState="false"
  AutoEventWireup="false"
%><!DOCTYPE html PUBLIC PUBLIC "-//W3C//DTD HTML 4.01//EN" "http://www.w3.org/TR/html4/strict.dtd">
<html>
<head runat="server">
<script runat="server">
//
// Copyright (C) 2006-2008 Tom Surace.
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
//
</script>
<title>T.A.M. Jukebox</title>

<script type="text/javascript" src="tjbfunctions.ashx?proxy"></script>
<script type="text/javascript" src="js/json.js"></script>
<script type="text/javascript" src="js/dojo/dojo.js" djConfig="parseOnLoad:true, isDebug:true, usePlainJson:true"></script>
<script type="text/javascript" src="index.aspx.js"></script>

<style type="text/css">
   @import "js/dijit/themes/tundra/tundra.css";
   @import "js/dojox/grid/_grid/tundraGrid.css";
   @import "js/dojo/resources/dojo.css";
</style>

<style type="text/css">
/* Fit to viewport, so that status bar will be at the bottom */
html, body {
  width: 100%; height: 100%;
  border: 0; padding: 0; margin: 0;
}

/* We do not use pixel sizes because we are not on crack. */
body {
   font-size: 10pt;
}

/* Doesn't work in comma list with html,body? Why? */
#mainFrame {
  width: 100%; height: 100%;
  border: 0; padding: 0; margin: 0;
}

table.nowPlaying {
  border: thin solid;
}

table.nowPlaying tr th {
  text-align: right;
}

#megaSuckBtn {
  background: red;
}

</style>

</head>
<body class="tundra">
<div dojoType="dijit.layout.BorderContainer" id="mainFrame">

<div dojoType="dijit.layout.ContentPane" id="content" region="center">

 <div id="moodBox" dojoType="dijit.layout.ContentPane">
  <table class="suckTable">
  <tr>
    <th>Opinion of</th>
    <td colspan="2"><button dojoType="dijit.form.Button"
       id="login" 
       jsId="jsLogin"
       onClick="onLogin();">Log In</button></td>
  </tr>

  <tr>
    <th><span id="suckLevel">-</span>% suck</th>
    <td>
      <button dojoType="dijit.form.Button" id="ruleBtn" jsId="jsRuleBtn">
        Rule
        <script type="dojo/method" event="onClick">onRule()</script>
      </button>
      <button dojoType="dijit.form.Button" id="suckBtn" jsId="jsSuckBtn">
        Suck
        <script type="dojo/method" event="onClick">onSuck()</script>
      </button>
      <button dojoType="dijit.form.Button" id="megaSuckBtn" jsId="jsMegaSuckBtn">
        Mega-Suck
        <script type="dojo/method" event="onClick">onMegaSuck()</script>
      </button>
  </tr>

  <tr>
    <th><span id="moodLevel">-</span>% 
      <button dojoType="dijit.form.Button" id="moodBtn" jsId="jsMoodBtn">
        (current mood)
        <script type="dojo/method" event="onClick">onMood()</script>
      </button></th>
    </th>
    <td><button dojoType="dijit.form.Button" id="yesBtn" jsId="jsYesBtn">
        Yes
        <script type="dojo/method" event="onClick">onYes()</script>
      </button>
      <button dojoType="dijit.form.Button" id="noBtn" jsId="jsNoBtn">
        No
        <script type="dojo/method" event="onClick">onNo()</script>
      </button></td>
  </tr>
  </table>
 </div>

 <div id="nowPlayingBox" dojoType="dijit.layout.ContentPane">
  <!-- I say this wants a little table -->
  <table class="nowPlaying">
  <tr>
    <th>Title</th>
    <td><span id="title" class="widthLimit"></span> 
      (<span id="trackId"></span>)</td>
  </tr>
  <tr>
    <th>Artist</th>
    <td><span id="artist" class="widthLimit"></span></td>
  </tr>
  <tr>
    <th>Album</th>
    <td><span id="album" class="widthLimit"></span></td>
  </tr>
  <tr>
    <th>File</th>
    <td><span id="filename" class="widthLimit"></span></td>
  </tr>
  </table>
 </div>

 <div dojoType="dijit.TitlePane" open="false"
   title="The Past" style="width:100%">
   (The past goes here!)
 </div>
   (The present goes here!)
 <div dojoType="dijit.TitlePane" open="false"
   title="The Future" style="width:100%">
   (The future goes here!)
 </div>

</div><!-- content -->

 <div dojoType="dijit.Dialog" id="moodPopup" title="Set Mood" 
   jsId="jsMoodPopup"
   style="display:none;">
   <div jsId="jsMoodGrid" dojoType="dojox.Grid" 
     style="height: 19em; width: 40em;"
     onRowDblClick="onMoodRowClick"
     ></div>
   <button dojoType="dijit.form.Button" 
     onclick="onMoodSelectClick();">Select</button> 
   <button dojoType="dijit.form.Button" 
     onclick="jsMoodPopup.hide();">Cancel</button> 
 </div>

 <div dojoType="dijit.Dialog" id="loginPopup" title="Login" 
   jsId="jsLoginPopup"
   href="login.aspx"
   style="display:none;">
 </div>

 <div id="status" dojoType="dijit.layout.ContentPane" region="top"
     orientation="horizontal"
     sizerWidth="8"
     style="border:2px;" >
   <div dojoType="dijit.layout.BorderContainer" splitter="true" design="sidebar" style="height:1.5em;width:100%;">
    <div dojoType="dijit.layout.ContentPane" splitter="true" region="center">
      <div dojoType="dijit.ProgressBar"
          jsId="jsProgressBar" id="downloadProgress" 
          indeterminate="true"
          report="progressReport"
          style="width: 80%;"></div>
    </div>
    <div dojoType="dijit.layout.ContentPane" sizeShare="50" region="left"> TAM Jukebox - 
      <a href="http://tekhedd.is-a-geek.net:8000/tamjb.ogg">Tune In</a> (if I said you can)</div>
    <div dojoType="dijit.layout.ContentPane" region="right">
     <button dojoType="dijit.form.Button"
       id="refreshBtn" 
       onClick="refresh(true);">Refresh</button>
    </div>
   </div>
 </div>

</div><!-- frame -->
</body>
</html>

