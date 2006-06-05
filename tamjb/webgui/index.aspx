<%@ Page language="C#" 
  MasterPageFile="~/tamjb.master"
  Inherits="byteheaven.tamjb.webgui.index" 
  Codebehind="index.aspx.cs"
  EnableViewState="false" 
  AutoEventWireup="false"
  Title="T.A.M. Jukebox"
%>
<%@ Register Assembly="Anthem" Namespace="Anthem" TagPrefix="anthem" %>
<script runat="server">
//
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

 <div style="float: right;">
    <anthem:Button id="refreshButton" runat="server" text="Refresh"
       OnClick="_OnRefresh" />
 </div>

 <div id="nowPlayingBox">
  <h2>Now Playing</h2>

  <!-- I say this wants a little table -->
  <table class="nowPlaying">
  <tr>
    <th>Title</th>
    <td><anthem:Label cssclass="nowPlayingData" 
      id="nowTitle" runat="server" text="" /></td>
  </tr>
  <tr>
    <th>Artist</th>
    <td><anthem:Label cssclass="nowPlayingData" 
      id="nowArtist" runat="server" text="" /></td>
  </tr>
  <tr>
    <th>Album</th>
    <td><anthem:Label cssclass="nowPlayingData" 
      id="nowAlbum" runat="server" text="" /></td>
  </tr>
  <tr>
    <th>File</th>
    <td><anthem:Label cssclass="nowPlayingData" 
      id="nowFileName" runat="server" text="" /></td>
  </tr>
  </table>

 </div>

</asp:Content>

