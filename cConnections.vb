Public Class cConnections

    Public Function MainConnection() As String
        Dim gstrConnStr As String = "Data Source=tcp:sql2k802.discountasp.net;Initial Catalog=SQL2008_786647_crdata;Persist Security Info=True;User ID=xxx;Password=yyy"
        'Dim gstrConnStr As String = "Data Source=DESKTOP-8NFUC49\SQLEXPRESS;Initial Catalog=CJR2020;Persist Security Info=True;User ID=aaa;Password=bbb"

        Return gstrConnStr

    End Function
End Class
