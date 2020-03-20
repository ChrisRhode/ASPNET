Public Class PickColor
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        Dim S As SharedRoutines
        Dim li As ListItem
        Dim c As Drawing.Color
        Dim t As String
        Dim index As Integer
        Dim intMyColorID As Integer
        Dim intUsedColors() As Integer

        lblMessage.Text = ""

        If (IsPostBack) Then
            Exit Sub
        End If

        S = New SharedRoutines(0)

        intMyColorID = Convert.ToInt32(GetFromSession("DICE_PREFERRED_COLOR"))
        intUsedColors = GetUsedColors()

        intLastNdx = S.MaxUserColorNum
        For intNdx = 1 To intLastNdx
            li = New ListItem
            c = S.GetColorMapping(intNdx)
            t = c.ToString
            index = t.IndexOf("[")
            If (index = -1) Then
                Throw New Exception("Color name decode error")
            End If
            t = t.Substring(index + 1)
            t = t.Substring(0, t.Length - 1)
            If (isColorInList(intUsedColors, intNdx)) Then
                t &= " (Used by another player)"
                li.Enabled = False
            End If
            li.Text = t
            li.Value = intNdx.ToString
            If (intNdx = intMyColorID) Then
                li.Selected = True
            End If
            li.Attributes.CssStyle.Add(HtmlTextWriterStyle.Color, t)
            rbColorChoices.Items.Add(li)
            li = Nothing
        Next

    End Sub

    Protected Sub btnSelect_Click(sender As Object, e As EventArgs) Handles btnSelect.Click
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Dim intGameNum As Integer
        Dim intUserIDNum As Integer
        Dim intSelectedNdx As Integer
        Dim intSelectedColor As Integer
        Dim intReturnStatus As Integer

        If (btnSelect.Text = "Try Again!") Then
            Response.Redirect("PickColor.aspx", True)
        End If

        intSelectedNdx = rbColorChoices.SelectedIndex
        If (intSelectedNdx = -1) Then
            lblMessage.Text = "Please pick a color from the list"
            lblMessage.ForeColor = Drawing.Color.Red
            Exit Sub
        End If
        intSelectedColor = Convert.ToInt32(rbColorChoices.Items(intSelectedNdx).Value)

        intGameNum = Convert.ToInt32(GetFromSession("DICE_GAME_ID"))
        intUserIDNum = Convert.ToInt32(GetFromSession("DICE_PLAYER_ID"))

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[SetPlayerColor]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", intGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@UserIDNum", intUserIDNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@NewColorID", intSelectedColor))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            intReturnStatus = result
        Catch ex As Exception
            Throw New Exception("SetPlayerColor failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        If (intReturnStatus = 0) Then
            lblMessage.Text = "Somebody else must have just selected that color :("
            lblMessage.ForeColor = Drawing.Color.Red
            btnSelect.Text = "Try Again!"
        Else
            Response.Redirect("GetReady.aspx", True)
        End If
    End Sub

    Function GetUsedColors() As Integer()
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim DBrdr As SqlClient.SqlDataReader
        Dim CS As New cConnections

        Dim intUsed() As Integer

        Dim intGameNum As Integer
        Dim strLog As String = ""
        Dim intCtr As Integer = 0
        Dim intThisColor As Integer

        ReDim intUsed(8)

        intGameNum = Convert.ToInt32(GetFromSession("DICE_GAME_ID"))

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[GetColorsInUse]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", intGameNum))
            DBrdr = DBcmd.ExecuteReader
            Do While (DBrdr.Read)
                intThisColor = DBrdr.GetInt32(0)
                If (intThisColor <> 0) Then
                    intCtr += 1
                    intUsed(intCtr) = intThisColor
                End If
            Loop

            DBrdr.Close()
        Catch ex As Exception
            Throw New Exception("GetColorsInUse Failed", ex)

        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        ReDim Preserve intUsed(intCtr)
        Return intUsed
    End Function

    Function isColorInList(theList() As Integer, intTheColorID As Integer) As Boolean
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        intLastNdx = UBound(theList)
        If (intLastNdx = 0) Then
            Return False
        End If
        For intNdx = 1 To intLastNdx
            If (intTheColorID = theList(intNdx)) Then
                Return True
            End If
        Next

        Return False
    End Function
    Function GetFromSession(strVariableName As String) As String
        Dim strValue As String = ""

        If (Not IsNothing(Session(strVariableName))) Then '
            strValue = Session(strVariableName)
        End If

        Return strValue
    End Function
End Class