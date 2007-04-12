<%@ Page language="C#" 
  MasterPageFile="~/tamjb.master"
  Inherits="byteheaven.tamjb.webgui.index" 
  Codebehind="index.aspx.cs"
  EnableViewState="true"
  AutoEventWireup="false"
  Title="T.A.M. Jukebox Index"
%>
<%@ Register Assembly="Anthem" Namespace="Anthem" TagPrefix="anthem" %>
<script runat="server">
//
// $Id$
// Copyright (C) 2006-2007 Tom Surace.
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
<script type="text/javascript">

    function historyCommand(action,keystring)
    {
        StartUpdate();
        setTimeout('historyCommandPtTwo("' + action + '","' + keystring + '")', 1 );
    }

    function historyCommandPtTwo(action,keystring)
    {
        Anthem_InvokePageMethod('_OnHistoryCommand', [action,keystring], null);
        FinishUpdate();
    }

</script>

 <div style="float: right; margin: 0.6em;">
   <anthem:Timer id="refreshTimer" runat="server" 
     Enabled="true"
     Interval="5000"
     OnTick="_OnRefresh"
     />
    <anthem:Button id="refreshButton" runat="server" 
       cssclass="stdButton"
       text="-1"
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       OnClick="_OnRefresh" />
 </div>

 <div id="moodBox">
  <table class="suckTable">
  <tr>
    <th>Opinion of</th>
    <td colspan="2"><anthem:LinkButton id="userNameBtn" runat="server" text="(unknown)" 
        OnClick="_OnUserClick" /></td>
  </tr>

  <tr>
    <th>Suck Amount<br />
       <anthem:Label
         id="nowSuckLevel" runat="server" text="100" />%</th>
    <td><anthem:Button id="ruleBtn" runat="server" text="Rule"
       cssclass="stdButton"
       OnClick="_OnRule"
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       EnableDuringCallback="false"
       />
       <anthem:Button id="suckBtn" runat="server" text="Suck"
       cssclass="stdButton"
       OnClick="_OnSuck"
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       EnableDuringCallback="False" /></td>
  </tr>

  <tr>
    <th><anthem:LinkButton runat="server" 
       id="moodBtn" 
       EnableCallback="false"
       text="(unknown)" 
       OnClick="_OnMoodClick" /><br />
       <anthem:Label cssclass="nowPlayingData"
        id="nowMoodLevel" runat="server" text="0" />%</th>
    </th>
    <td><anthem:Button id="yesBtn" runat="server" text="Yes"
       cssclass="stdButton"
       OnClick="_OnYes" 
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       EnableDuringCallback="false"
       />
       <anthem:Button id="noBtn" runat="server" text="No"
       cssclass="stdButton"
       OnClick="_OnNo" 
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       EnableDuringCallback="false"
       /></td>
  </tr>
  </table>
 </div>

 <div id="megaSuckBox">
  <anthem:Button id="megaSuckBtn" runat="server" text="Mega-Suck"
    cssclass="megaSuckBtn"
    OnClick="_OnMegaSuck"
    PreCallbackFunction="StartUpdate"
    PostCallbackFunction="FinishUpdate"
    EnableDuringCallback="false" />
 </div>

 <div id="transportBox">
  <table class="transportTable">
  <tr>
    <td><anthem:Button id="prevBtn" runat="server" text="Prev"
       OnClick="_OnPrev"
       cssclass="stdButton"
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       EnableDuringCallback="False" 
       /></td>
    <td><anthem:Button id="nextBtn" runat="server" text="Next"
       cssclass="stdButton"
       OnClick="_OnNext" 
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       EnableDuringCallback="false"
       /></td>
    <td><anthem:Button id="stopBtn" runat="server" text="Stop"
       cssclass="stdButton"
       OnClick="_OnStop" 
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       EnableDuringCallback="false"
       /></td>
    <td><anthem:Button id="playBtn" runat="server" text="Play"
       cssclass="stdButton"
       OnClick="_OnPlay" 
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
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
  <tr>
    <th>&nbsp;</th>
    <td><anthem:CheckBox id="showHistory" runat="server" 
     Text="History Enabled" 
     EnableDuringCallback="false"
     PreCallbackFunction="StartUpdate"
     PostCallbackFunction="FinishUpdate"
     Checked="false"
     AutoCallback="true"
     /></td>
  </tr>
  </table>
 </div>

 <anthem:Panel id="historyBox" runat="server" 
     cssclass="historyBox" >

  <anthem:Repeater id="history" runat="server" 
    EnableViewState="false"
    >
    <HeaderTemplate>
      <table id="historyTable" class="history">
      <thead>
      <tr>
        <th>Title</th>
        <th>Artist</th>
        <th>Album</th>
        <th>Suck</th>
        <th>Mood</th>
      </tr>
      </thead>
      <tbody>
    </HeaderTemplate>

    <FooterTemplate>
      </tbody>
      </table>
    </FooterTemplate>

    <ItemTemplate>
      <tr class='<%# DataBinder.Eval(Container.DataItem, "when") %> <%# DataBinder.Eval(Container.DataItem, "status") %> <%# DataBinder.Eval(Container.DataItem, "probability" ) %>' >

      <td><div class="widthLimit"><%# DataBinder.Eval(Container.DataItem, 
          "title") %></div></td>

      <td><div class="widthLimit"><%# DataBinder.Eval(Container.DataItem, 
          "artist") %></div></td>

      <td><div class="widthLimit"><%# DataBinder.Eval(Container.DataItem, 
          "album") %></div></td>

      <td class="suck"><div class="suck"><%# DataBinder.Eval(Container.DataItem, "suck") %>%
        <a href="#"
           OnClick='<%# "javascript:historyCommand(\"suckMore\",\"" + DataBinder.Eval(Container.DataItem, "key") + "\"); return false;" %>' 
           >Suck</a>
        |
        <a href="#"
           OnClick='<%# "javascript:historyCommand(\"suckLess\",\"" + DataBinder.Eval(Container.DataItem, "key") + "\"); return false;" %>' 
           >Rule</a></div></td>

      <td class="mood"><div class="mood"><%# DataBinder.Eval(Container.DataItem, "Mood") %>%
        <a href="#"
           OnClick='<%# "javascript:historyCommand(\"moodNo\",\"" + DataBinder.Eval(Container.DataItem, "key") + "\"); return false;" %>' 
           >No</a>
        |
        <a href="#"
           OnClick='<%# "javascript:historyCommand(\"moodYes\",\"" + DataBinder.Eval(Container.DataItem, "key") + "\"); return false;" %>' 
           >Yes</a></div></td>

      </tr>
    </ItemTemplate>
  </anthem:Repeater>
 </anthem:Panel>

</asp:Content>

