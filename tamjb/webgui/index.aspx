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
   @import "js/dojo/resources/dojo.css";

/* Fit to viewport, so that status bar will be at the bottom */
html, body {
  width: 100%; height: 100%;
  border: 0; padding: 0; margin: 0;
}

/* Doesn't work in comma list with html,body? Why? */
#mainFrame {
  width: 100%; height: 100%;
  border: 0; padding: 0; margin: 0;
}
</style>

</head>
<body class="tundra">
<div dojoType="dijit.layout.BorderContainer" id="mainFrame">

<div dojoType="dijit.layout.ContentPane" id="content" region="center">

 <div style="float: right; margin: 0.6em;">
    <button id="refreshButton" onClick="forceRefresh();">Refresh</button>
 </div>

 <div id="moodBox" dojoType="dijit.layout.ContentPane">
  <table class="suckTable">
  <tr>
    <th>Opinion of</th>
    <td colspan="2"><a href="#">(unknown user)</a></td>
  </tr>

  <tr>
    <th>Suck Amount<br /><span id="suckLevel">(suck)</span>%</th>
    <td>
      <button dojoType="dijit.form.Button" id="ruleBtn">
        Rule
        <script type="dojo/method" event="onClick">onRule()</script>
      </button>
      <button dojoType="dijit.form.Button" id="suckBtn">
        Suck
        <script type="dojo/method" event="onClick">onSuck()</script>
      </button>
  </tr>

  <tr>
    <th><button dojoType="dijit.form.Button" id="moodBtn">
        (current mood)
        <script type="dojo/method" event="onMood">onMood()</script>
        </button><br />
        <span id="moodLevel">(mood)</span>%</th>
    </th>
    <td>Yes | No</td>
  </tr>
  </table>
 </div>

 <div id="megaSuckBox" dojoType="dijit.layout.ContentPane">
  <button dojoType="dijit.form.Button" id="megaSuckBtn">
    Mega-Suck
    <script type="dojo/method" event="onClick">onMegaSuck()</script>
  </button>
 </div>

 <!-- TODO: allow this to be visible for the MASTER login -->
 <div id="transportBox">
  <table class="transportTable">
  <tr>
    <td>Prev</td>
    <td>Next</td>
    <td>Stop</td>
    <td>Play</td>
  </tr>
  </table>
 </div>

 <div id="nowPlayingBox" dojoType="dijit.layout.ContentPane">
  <!-- I say this wants a little table -->
  <table class="nowPlaying">
  <tr>
    <th>Title</th>
    <td>(<span id="trackId"></span>) <span id="title" class="widthLimit"></span></td>
  </tr>
  <tr>
    <th>Artist</th>
    <td><span id="artist"> class="widthLimit"></span></td>
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

 <div dojoType="dijit.TitlePane" open="true"
   title="The Past" style="width:100%">
   Why a title pane?
 </div>
</div><!-- content -->

 <div id="status" dojoType="dijit.layout.ContentPane" region="bottom"
     orientation="horizontal"
     sizerWidth="8"
     style="border:2px;" >
   <div dojoType="dijit.layout.BorderContainer" splitter="true" design="sidebar" style="height:1.5em;width:100%;">
    <div dojoType="dijit.layout.ContentPane" sizeShare="50" splitter="true" region="center">
      <div dojoType="dijit.ProgressBar"
          jsId="jsProgressBar" id="downloadProgress" 
          indeterminate="true"
          report="progressReport"></div>
    </div>
    <div dojoType="dijit.layout.ContentPane" sizeShare="50" region="left"> TAM Jukebox - 
      <a href="#">Tune In</a></div>
   </div>
 </div>

</div><!-- frame -->
</body>
</html>

