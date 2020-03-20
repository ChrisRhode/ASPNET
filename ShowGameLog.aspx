<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="ShowGameLog.aspx.vb" Inherits="AddOn2019.ShowGameLog" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
     <meta name="viewport" content="width=device-width,initial-scale=1.0" />
    <script type="text/javascript">
         // thanks Internet ... this will force the textbox to scroll to end
        window.onload = function()
        {
            var tmp = document.getElementById('<%=tbLog.ClientID %>');
            tmp.scrollTop = tmp.scrollHeight;
        }
    </script>
    <form id="form1" runat="server">
        <div>
            <asp:Button ID="btnGoBack" runat="server" Text="Back to game" /><br />
            <asp:TextBox ID="tbLog" runat="server" Height="246px" ReadOnly="True" TextMode="MultiLine" Width="609px"></asp:TextBox>
        </div>
        
    </form>
</body>
</html>
