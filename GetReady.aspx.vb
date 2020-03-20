Public Class GetReady
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim DBrdr As SqlClient.SqlDataReader
        Dim CS As New cConnections

        Dim intGameNum As Integer
        Dim intCtr As Integer = 0
        Dim strWaitingNames As String = ""

        intGameNum = Convert.ToInt32(GetFromSession("DICE_GAME_ID"))

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[CheckNotReadyPlayers]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", intGameNum))
            DBrdr = DBcmd.ExecuteReader
            Do While (DBrdr.Read)
                intCtr += 1
                If (strWaitingNames <> "") Then
                    strWaitingNames &= " and "
                Else
                    strWaitingNames = "Waiting on "
                End If
                strWaitingNames &= DBrdr.GetString(1)
            Loop

            DBrdr.Close()
        Catch ex As Exception
            Throw New Exception("CheckNotReadyPlayers Failed", ex)

        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        If (intCtr = 0) Then
            Response.Redirect("DiceActivePlayer.aspx", True)
        Else
            lblMessage.Text = strWaitingNames
            Response.AppendHeader("Refresh", "10")
        End If
    End Sub

    Function GetFromSession(strVariableName As String) As String
        Dim strValue As String = ""

        If (Not IsNothing(Session(strVariableName))) Then '
            strValue = Session(strVariableName)
        End If

        Return strValue
    End Function
End Class