<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="DiceNewGame.aspx.vb" Inherits="AddOn2019.DiceNewGame" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <meta name="viewport" content="width=device-width,initial-scale=1.0" />
    <form id="form1" runat="server">
        <div>
            <asp:Label ID="Label1" runat="server" Text="Pick players for new game"></asp:Label><p></p>
            <asp:PlaceHolder ID="ph" runat="server"></asp:PlaceHolder><br />
            <asp:Button ID="btnSetUpNewGame" runat="server" Text="Set Up New Game" /><p></p>
            <asp:CheckBox ID="cbEnableDebugOptions" runat="server" Text="Debug Mode" /><br />
            <asp:Button ID="btnGoToGame" runat="server" Text="Go To A Game" />
            
            <p></p>
            <asp:Label ID="lblMessage" runat="server" Text="Label"></asp:Label>
        </div>
      
    </form>
</body>
</html>
