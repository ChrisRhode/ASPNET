<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="PickColor.aspx.vb" Inherits="AddOn2019.PickColor" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
     <meta name="viewport" content="width=device-width,initial-scale=1.0" />
    <form id="form1" runat="server">
        <div>
            <asp:Label ID="Label1" runat="server" Text="Select your desired dice color"></asp:Label><p></p>
            <p>
                <asp:RadioButtonList ID="rbColorChoices" runat="server">
                </asp:RadioButtonList>
            </p>
            <asp:Button ID="btnSelect" runat="server" Text="Use this color!" /><p></p>
           <asp:Label ID="lblMessage" runat="server" Text="Label"></asp:Label>
        </div>
        
    </form>
</body>
</html>
