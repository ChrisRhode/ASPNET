<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="DiceActivePlayer.aspx.vb" Inherits="AddOn2019.DiceActivePlayer" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
     <meta name="viewport" content="width=device-width,initial-scale=1.0" />
    <form id="form1" runat="server">
        <div>
            <asp:Table ID="tblDisplay" runat="server">
            </asp:Table><p> </p>
                <asp:Table ID="tblPlayerInfo" runat="server">
                </asp:Table><p></p>
                <asp:Label ID="lblMessage" runat="server" Text="Label"></asp:Label><br />
            <asp:Table ID="tblCurrPlayerDice" runat="server">
                </asp:Table><br />
            <asp:Button ID="btnRefresh" runat="server" Text="Refresh" />&nbsp;&nbsp;
            <asp:Button ID="btnLog" runat="server" Text="View Game Log" /><p>
                
            </p>
            <asp:RadioButtonList ID="rbLegalMoves" runat="server" AutoPostBack="True">
            </asp:RadioButtonList><p></p>
            <asp:Button ID="btnSelect" runat="server" Text="Select this Move" Visible="False" />&nbsp;&nbsp;
            <asp:Button ID="btnUnselect" runat="server" Text="Unselect this Move" Visible="False" /><br />
            <asp:Button ID="btnCommit" runat="server" Text="Commit this Move" style="height: 29px" /><p></p>
            <asp:Button ID="btnChangePlayer" runat="server" Text="Change Player" />
            <asp:CheckBox ID="cbAutoChangePlayer" runat="server" Text="Auto Change To Next Player" />
        </div>
        
    </form>
</body>
</html>
