<%@ Page language="C#" 
  MasterPageFile="~/tamjb.master"
  Inherits="byteheaven.tamjb.webgui.moodselect" 
  Codebehind="index.aspx.cs"
  EnableViewState="true"
  AutoEventWireup="false"
  Title="T.A.M. Jukebox - Mood Select"
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

<asp:Content id="content" contentplaceholderid="mainContent" runat="server" >
 <div id="moodSelectBox">
  Moods for <asp:Literal runat="server" id="currentUserBox" />:

  <anthem:Repeater id="moodSelect" runat="server" >
    <HeaderTemplate>
      <div id="moodSelect">
    </HeaderTemplate>

    <FooterTemplate>
      </div>
    </FooterTemplate>

    <ItemTemplate>
      <div class='moodEntry <%# DataBinder.Eval(Container.DataItem, "status") %>' >
        <a href="moodselect.aspx?mood=<%# DataBinder.Eval(Container.DataItem, "moodKey") %>&action=select" ><%# DataBinder.Eval(Container.DataItem, "moodName") %></a>
      </div>
    </ItemTemplate>
  </anthem:Repeater>
 </div>

 <div id="moodCreateBox">
   <anthem:TextBox id="newMoodBox" runat="server"
      Columns="20" />
   <anthem:Button id="createBtn" runat="server" text="Create"
       cssclass="stdButton"
       OnClick="_OnCreate" 
       PreCallbackFunction="StartUpdate"
       PostCallbackFunction="FinishUpdate"
       EnableDuringCallback="false"
       />
 </div>
</asp:Content>

