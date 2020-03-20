Public Class Dice
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If (Not IsPostBack) Then
            lblErrorMessages.Text = ""
        End If
    End Sub

    Protected Sub btnEnterGame_Click(sender As Object, e As EventArgs) Handles btnEnterGame.Click
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim DBrdr As SqlClient.SqlDataReader
        Dim CS As New cConnections

        Dim strUserID As String
        Dim intGameNum As Integer
        Dim intColorID As Integer
        Dim intPreferredColorID As Integer

        lblErrorMessages.Text = ""
        lblErrorMessages.ForeColor = Drawing.Color.Red

        strUserID = txtUserID.Text.Trim

        If (strUserID = "") Then
            lblErrorMessages.Text = "Please enter a UserID"
            Exit Sub
        End If

        If (Not IsNumeric(txtGameNum.Text.Trim)) Then
            lblErrorMessages.Text = "Please enter a numeric Game Number"
            Exit Sub
        End If

        intGameNum = Convert.ToInt32(txtGameNum.Text.Trim)

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[GetGameForUser]"
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", intGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@UserID", strUserID))
            DBrdr = DBcmd.ExecuteReader
            If (Not DBrdr.Read) Then
                Throw New Exception("Game could not be found")
            Else
                If (IsDBNull(DBrdr.Item(0))) Then
                    Throw New Exception("Unexpected error in GetGameForUser")
                ElseIf (DBrdr.GetInt32(0) = -1) Then
                    Throw New Exception("User could not be found")
                ElseIf (IsDBNull(DBrdr.Item(0))) Then
                    Throw New Exception("User not found in game")
                End If

                Session.Add("DICE_GAME_ID", Convert.ToString(intGameNum))
                Session.Add("DICE_PLAYER_ID", Convert.ToString(DBrdr.GetInt32(0)))
                Session.Add("DICE_PLAYER_NAME", strUserID)
                intColorID = DBrdr.GetInt32(3)
                intPreferredColorID = DBrdr.GetInt32(4)
            End If
            DBrdr.Close()
        Catch ex As Exception
            lblErrorMessages.Text = ex.Message
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        If (intColorID <> 0) Then
            Response.Redirect("GetReady.aspx", True)
        Else
            Session.Add("DICE_PREFERRED_COLOR", intColorID.ToString)
            Response.Redirect("PickColor.aspx", True)
        End If

    End Sub

End Class