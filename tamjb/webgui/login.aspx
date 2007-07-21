<%@ Page language="C#" 
  EnableViewState="True" 
  Inherits="byteheaven.tamjb.webgui.login" 
 %><html>
<head>
<style type="text/css">

</style>

<title>Yo!</title>
</head>
<body>
<form runat="server" id="mainForm">
  
<asp:Panel runat="server" id="errorBox" class="errorBox">
  <asp:Label runat="server" id="errorMsg" />
</asp:Panel>
<asp:Panel runat="server" id="loginBox" class="loginBox">
  <h2>Identify</h2>
  <span class="label">ID:</span>
  <asp:TextBox runat="server" id="idBox" columns="20" />
  <br />
  <span class="label">Password:</span>
  <asp:TextBox 
      runat="server" TextMode="password" id="passwordBox" 
      columns="20" />
  <br />
  <asp:Button runat="server" Text="Identify" id="submit"
      EnableViewState="False" />
</asp:Panel>
</form>


<script language="javascript" type="text/javascript">
<!-- 
document.getElementById("passwordBox").focus();
// -->
</script>
</body>
</html>
