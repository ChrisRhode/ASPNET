<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="Dice.aspx.vb" Inherits="AddOn2019.Dice" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <meta name="viewport" content="width=device-width,initial-scale=1.0" />
    <form id="form1" runat="server">
        <div>
            Enter your ID:&nbsp;&nbsp;<asp:TextBox ID="txtUserID" runat="server"></asp:TextBox><br />
            Game Number:&nbsp;&nbsp;<asp:TextBox ID="txtGameNum" runat="server" Width="59px"></asp:TextBox><p></p>
            <asp:Button ID="btnEnterGame" runat="server" Text="Enter Game" Width="156px" /><br />
            <asp:Label ID="lblErrorMessages" runat="server" Text="Label"></asp:Label>
        </div>
    </form>
</body>
</html>
