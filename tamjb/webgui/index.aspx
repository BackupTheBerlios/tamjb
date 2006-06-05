<%@ Page language="C#" 
  MasterPageFile="~/tamjb.master"
  Inherits="byteheaven.tamjb.webgui.index" 
  Codebehind="index.aspx.cs"
  EnableViewState="true"
  AutoEventWireup="false"
  Title="T.A.M. Jukebox"
%>
<%@ Register Assembly="Anthem" Namespace="Anthem" TagPrefix="anthem" %>
<script runat="server">
//
// $Id$
// Copyright (C) 2006 Tom Surace.
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

<asp:Content id="content" contentplaceholderid="mainContent" runat="server">
 <div style="float: right; margin: 0.6em;">
    <anthem:Button id="refreshButton" runat="server" 
       cssclass="stdButton"
       text="Refresh"
       OnClick="_OnRefresh" />
 </div>

 <div id="moodBox">
  <table class="suckTable">
  <tr>
    <th><anthem:LinkButton id="userNameBtn" runat="server" text="(unknown)" 
      OnClick="_OnUserClick" /></th>
    <td><anthem:Button id="ruleBtn" runat="server" text="Rule"
       cssclass="stdButton"
       OnClick="_OnRule"
       EnableDuringCallback="false"
       /></td>
    <td><div class="widthLimit"><anthem:Label cssclass="nowPlayingData"
      id="nowSuckLevel" runat="server" text="100" />%</div></td>
    <td><anthem:Button id="suckBtn" runat="server" text="Suck"
       cssclass="stdButton"
       OnClick="_OnSuck"
       EnableDuringCallback="False" /></td>
  </tr>

  <tr>
    <th><anthem:LinkButton id="moodBtn" runat="server" text="(unknown)" 
       OnClick="_OnMoodClick" /></th>
    <td><anthem:Button id="yesBtn" runat="server" text="Yes"
       cssclass="stdButton"
       OnClick="_OnYes" 
       EnableDuringCallback="false"
       /></td>
    <td><div class="widthLimit"><anthem:Label cssclass="nowPlayingData"
      id="nowMoodLevel" runat="server" text="0" />%</div></td>
    <td><anthem:Button id="noBtn" runat="server" text="No"
       cssclass="stdButton"
       OnClick="_OnNo" 
       EnableDuringCallback="false"
       /></td>
  </tr>
  </table>
 </div>

 <div id="transportBox">
  <table class="transportTable">
  <tr>
    <td><anthem:Button id="prevBtn" runat="server" text="Prev"
       OnClick="_OnPrev" TextDuringCallback="Updating" 
       cssclass="stdButton"
       EnableDuringCallback="False" 
       /></td>
    <td><anthem:Button id="nextBtn" runat="server" text="Next"
       cssclass="stdButton"
       OnClick="_OnNext" 
       EnableDuringCallback="false"
       /></td>
    <td><anthem:Button id="stopBtn" runat="server" text="Stop"
       cssclass="stdButton"
       OnClick="_OnStop" 
       EnableDuringCallback="false"
       /></td>
    <td><anthem:Button id="playBtn" runat="server" text="Play"
       cssclass="stdButton"
       OnClick="_OnPlay" 
       EnableDuringCallback="false"
       /></td>
  </tr>
  </table>
 </div>

 <div id="nowPlayingBox">
  <!-- I say this wants a little table -->
  <table class="nowPlaying">
  <tr>
    <th>Title</th>
    <td><div class="widthLimit"><anthem:Label cssclass="nowPlayingData" 
      id="nowTitle" runat="server" text="(unknown)" /></div></td>
  </tr>
  <tr>
    <th>Artist</th>
    <td><div class="widthLimit"><anthem:Label cssclass="nowPlayingData" 
      id="nowArtist" runat="server" text="" /></div></td>
  </tr>
  <tr>
    <th>Album</th>
    <td><div class="widthLimit"><anthem:Label cssclass="nowPlayingData" 
      id="nowAlbum" runat="server" text="" /></div></td>
  </tr>
  <tr>
    <th>File</th>
    <td><div class="widthLimit"><anthem:Label cssclass="nowPlayingData" 
      id="nowFileName" runat="server" text="" /></div></td>
  </tr>
  </table>

 </div>

 <div id="historyBox">
  <anthem:Repeater id="history" runat="server" 
    EnableViewState="false"
    OnItemCommand="_OnHistoryCommand" >
    <HeaderTemplate>
      <table id="historyTable" class="history">
      <thead>
      <tr>
        <th>Title</th>
        <th>Artist</th>
        <th>Album</th>
        <th>Suck</th>
        <th>Mood</th>
        <th>Status</th>
      </tr>
      </thead>
      <tbody>
    </HeaderTemplate>

    <FooterTemplate>
      </tbody>
      </table>
    </FooterTemplate>

    <ItemTemplate>
      <tr class="<%# DataBinder.Eval(Container.DataItem, "when") %>">
      <td><div class="widthLimit"><%# DataBinder.Eval(Container.DataItem, 
          "title") %></div></td>

      <td><div class="widthLimit"><%# DataBinder.Eval(Container.DataItem, 
          "artist") %></div></td>

      <td><div class="widthLimit"><%# DataBinder.Eval(Container.DataItem, 
          "album") %></div></td>

      <td><div class="suckOrMood"><%# DataBinder.Eval(Container.DataItem, "suck") %>%
<anthem:LinkButton runat="server" CommandName="suckMore" 
  CommandArgument='<%# DataBinder.Eval(Container.DataItem, "key") %>'
  Text="Suck" />
  |
<anthem:LinkButton runat="server" CommandName="suckLess" 
  CommandArgument='<%# DataBinder.Eval(Container.DataItem, "key") %>'
  Text="Rule" /></div></td>

      <td><div class="suckOrMood"><%# DataBinder.Eval(Container.DataItem, "Mood") %>%
<anthem:LinkButton runat="server" CommandName="moodYes" 
  CommandArgument='<%# DataBinder.Eval(Container.DataItem, "key") %>'
  Text="Yes" />
  |
<anthem:LinkButton runat="server" CommandName="moodNo" 
  CommandArgument='<%# DataBinder.Eval(Container.DataItem, "key") %>'
  Text="No" /></div></td>

      <td><div class="widthLimit"><%# DataBinder.Eval(Container.DataItem, "Status") %></div></td>
      </tr>
    </ItemTemplate>
  </anthem:Repeater>
 </div>

</asp:Content>

