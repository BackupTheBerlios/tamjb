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

  <anthem:Repeater id="moodSelect" runat="server"
    OnItemCommand="_OnMoodCommand" 
    PreCallbackFunction="StartUpdate"
    PostCallbackFunction="FinishUpdate"
    >
    <HeaderTemplate>
      <div id="moodSelect">
    </HeaderTemplate>

    <FooterTemplate>
      </div>
    </FooterTemplate>

    <ItemTemplate>
      <a href="#"
        OnClick='<%# "javascript:historyCommand(\"suckMore\",\"" 
          + DataBinder.Eval(Container.DataItem, "moodKey") 
          + "\"); return false;" %>' 
           ></a>

      <div class='moodEntry <%# DataBinder.Eval(Container.DataItem, "status") %>' >
        <anthem:LinkButton runat="server" id="selectBtn"
          CommandName="select"
          CommandArgument='<%# DataBinder.Eval(Container.DataItem, "moodKey") %>'
          Text='<%# DataBinder.Eval(Container.DataItem, "moodName") %>' 
    PreCallbackFunction="StartUpdate"
    PostCallbackFunction="FinishUpdate"
          />
      </div>
    </ItemTemplate>
  </anthem:Repeater>
 </div>
</asp:Content>

