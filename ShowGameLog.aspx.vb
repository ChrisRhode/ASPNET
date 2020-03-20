Public Class ShowGameLog
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim DBrdr As SqlClient.SqlDataReader
        Dim CS As New cConnections

        Dim intGameNum As Integer
        Dim strLog As String = ""

        intGameNum = Convert.ToInt32(GetFromSession("DICE_GAME_ID"))

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[GetLog]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", intGameNum))
            DBrdr = DBcmd.ExecuteReader
            Do While (DBrdr.Read)
                strLog &= DBrdr.GetString(0) & vbCrLf
            Loop

            DBrdr.Close()
        Catch ex As Exception
            Throw New Exception("GetLog Failed", ex)

        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        tbLog.Text = strLog

    End Sub
    Protected Sub btnGoBack_Click(sender As Object, e As EventArgs) Handles btnGoBack.Click
        Response.Redirect("DiceActivePlayer.aspx", True)
    End Sub
    Function GetFromSession(strVariableName As String) As String
        Dim strValue As String = ""

        If (Not IsNothing(Session(strVariableName))) Then '
            strValue = Session(strVariableName)
        End If

        Return strValue
    End Function
End Class