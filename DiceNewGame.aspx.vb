Public Class DiceNewGame
    Inherits System.Web.UI.Page

    Dim gintNewGameID As Integer
    Private Sub DiceNewGame_Init(sender As Object, e As EventArgs) Handles Me.Init
        ' render all the controls always
        Dim cblist As New CheckBoxList
        Dim cbitem As ListItem

        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim DBrdr As SqlClient.SqlDataReader
        Dim CS As New cConnections

        Dim strPlayerName As String
        Dim intPlayerID As Integer

        cblist.ID = "playerList"
        '' ** look at multiple invokes of init
        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[GetValidUserNames]"
            DBcmd.Parameters.Clear()
            DBrdr = DBcmd.ExecuteReader
            Do While (DBrdr.Read)
                strPlayerName = DBrdr.GetString(0)
                intPlayerID = DBrdr.GetInt32(1)
                cbitem = New ListItem
                cbitem.Text = strPlayerName
                cbitem.Value = Convert.ToString(intPlayerID)
                cblist.Items.Add(cbitem)
            Loop

            DBrdr.Close()
        Catch ex As Exception
            Throw New Exception("Player List read Failed", ex)

        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        ph.Controls.Add(cblist)
    End Sub
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        lblMessage.Text = ""
        If (Not IsPostBack) Then
            If (GetFromSession("DICE_DEBUG_MODE") <> "") Then
                cbEnableDebugOptions.Checked = True
            End If
        End If
    End Sub
    Protected Sub btnSetUpNewGame_Click(sender As Object, e As EventArgs) Handles btnSetUpNewGame.Click
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections
        Dim S As SharedRoutines
        Dim intNdx As Integer '
        Dim intLastNdx As Integer
        Dim cblist As CheckBoxList
        Dim intNewPlayerID As Integer
        Dim intAutoTurnSeqNum As Integer = 0
        Dim intPlayerCnt As Integer = 0

        cblist = CType(FindControl("playerList"), CheckBoxList)
        intLastNdx = cblist.Items.Count - 1
        For intNdx = 0 To intLastNdx
            If (cblist.Items(intNdx).Selected) Then
                intPlayerCnt += 1
            End If
        Next

        If (intPlayerCnt = 0) Then
            lblMessage.Text = "Please select at least one player"
            lblMessage.ForeColor = Drawing.Color.Red
            Exit Sub
        End If

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[GetNewGameID]"
            DBcmd.Parameters.Clear()
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            gintNewGameID = DBcmd.Parameters("RETURN_VALUE").Value
        Catch ex As Exception
            Throw New Exception("Get New Game ID Failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        For intNdx = 0 To intLastNdx
            If (cblist.Items(intNdx).Selected) Then
                intNewPlayerID = Convert.ToInt32(cblist.Items(intNdx).Value)
                ''
                Try
                    DBcon.ConnectionString = CS.MainConnection
                    DBcon.Open()
                    DBcmd.Connection = DBcon
                    DBcmd.CommandType = CommandType.StoredProcedure
                    DBcmd.CommandText = "[Dice].[AddPlayerToGame]"
                    DBcmd.Parameters.Clear()
                    DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintNewGameID))
                    DBcmd.Parameters.Add(New SqlClient.SqlParameter("@UserIDNum", intNewPlayerID))
                    intAutoTurnSeqNum += 1
                    DBcmd.Parameters.Add(New SqlClient.SqlParameter("@TurnSeqNum", intAutoTurnSeqNum))
                    Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
                    intRetParam.Direction = ParameterDirection.ReturnValue
                    DBcmd.Parameters.Add(intRetParam)
                    DBcmd.ExecuteNonQuery()
                    Dim result As Integer
                    result = DBcmd.Parameters("RETURN_VALUE").Value
                    If (result <> 1) Then
                        Throw New Exception("Unexpected return value from Add Player To Game")
                    End If
                Catch ex As Exception
                    Throw New Exception("Add new player to game failed", ex)
                Finally
                    If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
                End Try
                ''
            End If
        Next

        '' set up casinos
        S = New SharedRoutines(gintNewGameID)
        S.SetUpCasinos()
        ''
        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[MakeGameActive]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintNewGameID))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            If (result <> 1) Then
                Throw New Exception("Unexpected return value from Make Game Active")
            End If
        Catch ex As Exception
            Throw New Exception("Make Game Active Failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        lblMessage.Text = "Game created, ID: " & gintNewGameID
        lblMessage.ForeColor = Drawing.Color.Green
    End Sub

    Private Sub cbEnableDebugOptions_CheckedChanged(sender As Object, e As EventArgs) Handles cbEnableDebugOptions.CheckedChanged
        If (cbEnableDebugOptions.Checked) Then
            Session.Add("DICE_DEBUG_MODE", "YES")
        Else
            Session.Remove("DICE_DEBUG_MODE")
        End If
    End Sub

    Protected Sub btnGoToGame_Click(sender As Object, e As EventArgs) Handles btnGoToGame.Click
        Response.Redirect("Dice.aspx")
    End Sub

    Function GetFromSession(strVariableName As String) As String
        Dim strValue As String = ""

        If (Not IsNothing(Session(strVariableName))) Then '
            strValue = Session(strVariableName)
        End If

        Return strValue
    End Function
End Class