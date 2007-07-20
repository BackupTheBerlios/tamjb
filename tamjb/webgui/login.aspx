<%@ Page language="C#" 
  EnableViewState="True" 
 %><%

// $Id$
// A simple login page, for a simple app. :)

%>
<script runat="server">
override protected void OnLoad( EventArgs loadArgs )
{
   base.OnLoad( loadArgs );

   if (! Page.IsPostBack)
   {
      errorBox.Visible = false;
   }
   else
   {
      string id = idBox.Text;
      string password = passwordBox.Text;

      // If no id was supplied, don't bail.
      if ("" == id)
         return;

      if (FormsAuthentication.Authenticate( id, password ))
         FormsAuthentication.RedirectFromLoginPage( id, false );
      else
         _Error( "Access Denied" );
   }
}

void _Error( string msg )
{
   errorBox.Visible = true;
   errorMsg.Text = msg;
}

</script>
<html>
<head>
<style type="text/css">
  // Hmm.
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
