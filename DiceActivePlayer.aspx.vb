Public Class DiceActivePlayer
    Inherits System.Web.UI.Page
    ' ** globalize variables properly
    Dim gintGameNum As Integer
    Dim gintPlayerNum As Integer
    Dim gstrPlayerName As String
    Dim gintDebugNewPlayerNum As Integer
    Dim gintCurrRoundNum As Integer
    Dim gboolGameEnded As Boolean = False

    Dim gds As DataSet

    Dim GS As SharedRoutines

    Enum eRollStatus
        HasNotRolledEver
        HasRolledForThisTurn
        HasNotRolledForThisTurn
    End Enum
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim intCurrPlayerSeqInTurn As Integer
        Dim intCurrPlayerID As Integer

        Dim RollStatus As eRollStatus

        If (Not IsPostBack) Then
            If (GetFromSession("DICE_AUTOCHANGEPLAYER") <> "") Then
                cbAutoChangePlayer.Checked = True
            End If
            If (GetFromSession("DICE_DEBUG_MODE") = "") Then
                cbAutoChangePlayer.Visible = False
                btnChangePlayer.Visible = False
            End If
        End If

        gintGameNum = Convert.ToInt32(GetFromSession("DICE_GAME_ID"))
        gintPlayerNum = Convert.ToInt32(GetFromSession("DICE_PLAYER_ID"))
        gstrPlayerName = GetFromSession("DICE_PLAYER_NAME")

        GS = New SharedRoutines(gintGameNum)

        tblCurrPlayerDice.Rows.Clear()

        LoadNeededData()

        SetCasinoDisplay()

        '' get info to display player's move choices, or "other player is playing"
        gintCurrRoundNum = gds.Tables("GameStatus").Rows(0).Item(0)
        If (gintCurrRoundNum = 5) Then
            Response.Redirect("ShowGameLog.aspx", True)
        End If
        intCurrPlayerSeqInTurn = gds.Tables("GameStatus").Rows(0).Item(1)
        intCurrPlayerID = GetPlayerIDByTurnSeqNum(intCurrPlayerSeqInTurn)

        lblMessage.ForeColor = Drawing.Color.Black
        lblMessage.Text = "Hello, " & gstrPlayerName & ".  Round: " & gintCurrRoundNum
        If (intCurrPlayerID = gintPlayerNum) Then
            lblMessage.Text &= " It is your turn!"

            RollStatus = GetHasRolledStatusForPlayerID(gintPlayerNum)
            If (RollStatus = eRollStatus.HasNotRolledEver) Then
                InitializePlayerDice()
                LoadNeededData()
                RollStatus = eRollStatus.HasNotRolledForThisTurn
            End If
            If (RollStatus = eRollStatus.HasNotRolledForThisTurn) Then
                RollPlayerDice()
                LoadNeededData()
            End If

            SetUpAllPlayersDisplay()
            SetUpCurrentPlayerDisplay()
        Else
            lblMessage.Text &= " It is " & PosessiveOf(GetPlayerNameFromID(intCurrPlayerID)) & " turn"
            btnSelect.Enabled = False
            btnUnselect.Enabled = False
            btnCommit.Enabled = False
            SetUpAllPlayersDisplay()
            Response.AppendHeader("Refresh", "10")
        End If

    End Sub

    Protected Sub btnSelect_Click(sender As Object, e As EventArgs) Handles btnSelect.Click
        Dim intNdx As Integer
        Dim intMoveOpCode As Integer
        Dim intMoveStatus As Integer

        intMoveStatus = GetSelectedMove()

        If (intMoveStatus <> 0) Then
            lblMessage.Text = "You already selected a move, use Unselect or Commit"
            lblMessage.ForeColor = Drawing.Color.Red
            Exit Sub
        End If

        intNdx = rbLegalMoves.SelectedIndex
        If (intNdx = -1) Then
            lblMessage.Text = "Please select a move"
            lblMessage.ForeColor = Drawing.Color.Red
            Exit Sub
        End If

        intMoveOpCode = rbLegalMoves.Items(intNdx).Value
        SetMoveWithUndo(intMoveOpCode)
    End Sub

    Protected Sub btnUnselect_Click(sender As Object, e As EventArgs) Handles btnUnselect.Click
        Dim intMoveStatus As Integer
        Dim intNdx As Integer '
        Dim intLastNdx As Integer

        intMoveStatus = GetSelectedMove()

        If (intMoveStatus = 0) Then
            lblMessage.Text = "You have not selected a move yet"
            lblMessage.ForeColor = Drawing.Color.Red
            Exit Sub
        End If

        UndoMove()

        intLastNdx = rbLegalMoves.Items.Count - 1
        For intNdx = 0 To intLastNdx
            rbLegalMoves.Items(intNdx).Selected = False
        Next

    End Sub

    Private Sub rbLegalMoves_SelectedIndexChanged(sender As Object, e As EventArgs) Handles rbLegalMoves.SelectedIndexChanged
        Dim intSelectedNdx As Integer
        Dim intMoveStatus As Integer
        Dim intMoveOpCode As Integer

        intSelectedNdx = rbLegalMoves.SelectedIndex 'Newly selected move
        intMoveStatus = GetSelectedMove()   'Previously selected move
        If (intMoveStatus <> -1) Then
            ' have to undo previous move
            UndoMove()
        End If

        intMoveOpCode = Convert.ToInt32(rbLegalMoves.Items(intSelectedNdx).Value)
        SetMoveWithUndo(intMoveOpCode)

    End Sub
    Protected Sub btnCommit_Click(sender As Object, e As EventArgs) Handles btnCommit.Click
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Dim intMoveStatus As Integer

        Dim intNdx As Integer
        Dim intLastNdx As Integer
        Dim strMessage As String = ""
        Dim intCountPlayerColor As Integer = 0
        Dim intCountWhiteColor As Integer = 0

        intMoveStatus = GetSelectedMove()

        If (intMoveStatus = 0) Then
            lblMessage.Text = "You have not selected a move yet"
            lblMessage.ForeColor = Drawing.Color.Red
            Exit Sub
        End If
        ' log the move that was done
        strMessage = gstrPlayerName & " placed dice on casino " & intMoveStatus & ": "

        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("DiceInHands").Rows(intNdx)
                If (.Item(0) = gintPlayerNum) Then
                    If (.Item(4)) Then
                        If (.Item(2) = -1) Then
                            intCountWhiteColor += 1
                        Else
                            intCountPlayerColor += 1
                        End If
                    End If
                End If
            End With
        Next
        If (intCountPlayerColor > 0) Then
            strMessage &= intCountPlayerColor & " own color dice"
        End If
        If (intCountWhiteColor > 0) Then
            If (intCountPlayerColor > 0) Then
                strMessage &= ", "
            End If
            strMessage &= intCountWhiteColor & " white dice"
        End If
        AddToLog(strMessage)

        ' physically remove the dice from player's "in hand" dice
        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[RemoveMovedDiceFromHand]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@UserIDNum", gintPlayerNum))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            If (result <> 1) Then
                Throw New Exception("Unexpected return value from RemoveMovedDiceFromHand")
            End If
        Catch ex As Exception
            Throw New Exception("Commit failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        ' advance the game to next player or round
        AdvanceTheTurn()
        If (gboolGameEnded) Then
            Response.Redirect("ShowGameLog.aspx", True)
        End If
        If (cbAutoChangePlayer.Checked) Then
            Session.Add("DICE_AUTOCHANGEPLAYER", "YES")
        Else
            Session.Remove("DICE_AUTOCHANGEPLAYER")
        End If
        If (gintDebugNewPlayerNum <> -1) And (cbAutoChangePlayer.Checked) Then
            Session.Add("DICE_PLAYER_ID", Convert.ToString(gintDebugNewPlayerNum))
            Session.Add("DICE_PLAYER_NAME", GetPlayerNameFromID(gintDebugNewPlayerNum))
        End If
        Response.Redirect("DiceActivePlayer.aspx", True)
    End Sub

    Protected Sub btnLog_Click(sender As Object, e As EventArgs) Handles btnLog.Click
        Response.Redirect("ShowGameLog.aspx", True)
    End Sub

    Protected Sub btnChangePlayer_Click(sender As Object, e As EventArgs) Handles btnChangePlayer.Click
        Response.Redirect("Dice.aspx", True)
    End Sub
    Sub SetMoveWithUndo(intOpCode As Integer)
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Dim intNdx As Integer
        Dim intLastNdx As Integer

        ' Move all dice with VALUE = intOpCode to Casinos

        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[MoveDieToCasino]"

            For intNdx = 0 To intLastNdx
                With gds.Tables("DiceInHands").Rows(intNdx)
                    If (.Item(0) = gintPlayerNum) And (.Item(1) = intOpCode) Then
                        DBcmd.Parameters.Clear()
                        DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
                        DBcmd.Parameters.Add(New SqlClient.SqlParameter("@UserIDNum", gintPlayerNum))
                        DBcmd.Parameters.Add(New SqlClient.SqlParameter("@DieSeqNum", .Item(3)))
                        DBcmd.Parameters.Add(New SqlClient.SqlParameter("@CasinoNum", intOpCode))
                        Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
                        intRetParam.Direction = ParameterDirection.ReturnValue
                        DBcmd.Parameters.Add(intRetParam)
                        DBcmd.ExecuteNonQuery()
                        Dim result As Integer
                        result = DBcmd.Parameters("RETURN_VALUE").Value
                        If (result <> 1) Then
                            Throw New Exception("Unexpected return value from MoveDieToCasino")
                        End If
                    End If
                End With
            Next
        Catch ex As Exception
            Throw New Exception("SetMoveWithUndo failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        ' need to update the display 
        LoadNeededData()
        SetCasinoDisplay()
        SetUpCurrentPlayerDisplay()
    End Sub

    Sub UndoMove()
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Dim intOpCode As Integer
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        intOpCode = GetSelectedMove()
        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[MoveDieFromCasino]"

            For intNdx = 0 To intLastNdx
                With gds.Tables("DiceInHands").Rows(intNdx)
                    If (.Item(0) = gintPlayerNum) And (.Item(1) = intOpCode) Then
                        DBcmd.Parameters.Clear()
                        DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
                        DBcmd.Parameters.Add(New SqlClient.SqlParameter("@UserIDNum", gintPlayerNum))
                        DBcmd.Parameters.Add(New SqlClient.SqlParameter("@DieSeqNum", .Item(3)))
                        DBcmd.Parameters.Add(New SqlClient.SqlParameter("@CasinoNum", intOpCode))
                        Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
                        intRetParam.Direction = ParameterDirection.ReturnValue
                        DBcmd.Parameters.Add(intRetParam)
                        DBcmd.ExecuteNonQuery()
                        Dim result As Integer
                        result = DBcmd.Parameters("RETURN_VALUE").Value
                        If (result <> 1) Then
                            Throw New Exception("Unexpected return value from MoveDieFromCasino")
                        End If
                    End If
                End With
            Next
        Catch ex As Exception
            Throw New Exception("UndoMove failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try

        ' need to update the display 
        LoadNeededData()
        SetCasinoDisplay()
        SetUpCurrentPlayerDisplay()
    End Sub
    ' ** recommended make select/unselect a toggle button

    Sub LoadNeededData()
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim DBAdapter As SqlClient.SqlDataAdapter

        Dim CS As New cConnections

        gds = New DataSet

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[GetDisplay]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
            DBAdapter = New SqlClient.SqlDataAdapter(DBcmd)
            DBAdapter.TableMappings.Clear()
            DBAdapter.TableMappings.Add("Table", "Banknotes")
            DBAdapter.TableMappings.Add("Table1", "Dice")
            DBAdapter.TableMappings.Add("Table2", "GameStatus")
            DBAdapter.TableMappings.Add("Table3", "DiceInHands")
            DBAdapter.TableMappings.Add("Table4", "PlayerDetails")
            DBAdapter.Fill(gds)
        Catch ex As Exception
            Throw New Exception("Data Load Failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try
    End Sub

    Sub AdvanceTheTurn()
        Dim intPlayerCnt As Integer
        Dim intScanPlayerID As Integer
        Dim intNbrTimes As Integer = 0
        Dim intNdx As Integer
        Dim intLastNdx As Integer
        Dim intPlayableDiceCnt As Integer
        Dim intCurrTurnSeqNum As Integer
        Dim intScanTurnSeqNum As Integer

        gintDebugNewPlayerNum = -1

        LoadNeededData()    'ensure loaded data is current

        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1

        intPlayerCnt = gds.Tables("PlayerDetails").Rows.Count
        intCurrTurnSeqNum = gds.Tables("GameStatus").Rows(0).Item(1)
        intScanTurnSeqNum = intCurrTurnSeqNum

        SetHasRolledStatus(False)

        Do While (True)
            intNbrTimes += 1
            If (intNbrTimes = intPlayerCnt + 1) Then
                '' no players have playabledice, end the round (check to see if have played 4 rounds too = end game)
                If (gintCurrRoundNum < 4) Then
                    ScoreTheRound()
                    SetGameState(2, 0)
                    GS.SetUpCasinos()
                Else
                    ScoreTheRound()
                    ScoreTheGame()
                    SetGameState(3, 0)
                    gboolGameEnded = True
                End If
                Exit Sub
            End If
            intScanTurnSeqNum += 1
            If (intScanTurnSeqNum > intPlayerCnt) Then
                intScanTurnSeqNum = 1
            End If
            intScanPlayerID = GetPlayerIDByTurnSeqNum(intScanTurnSeqNum)
            ' see how many playable dice this player has (or if has not rolled yet)
            If (GetHasRolledStatusForPlayerID(intScanPlayerID) = eRollStatus.HasNotRolledEver) Then
                ' this is the next player
                SetGameState(1, intScanTurnSeqNum)
                gintDebugNewPlayerNum = intScanPlayerID
                Exit Sub
            End If

            intPlayableDiceCnt = 0
            For intNdx = 0 To intLastNdx
                With gds.Tables("DiceInHands").Rows(intNdx)
                    If (.Item(0) = intScanPlayerID) And (.Item(1) <> 999) Then
                        intPlayableDiceCnt += 1
                    End If
                End With
            Next
            If (intPlayableDiceCnt > 0) Then
                ' this is the next player
                SetGameState(1, intScanTurnSeqNum)
                gintDebugNewPlayerNum = intScanPlayerID
                Exit Sub
            End If
        Loop

    End Sub

    Function CurrentScoreRankingsForCasino(intCasinoID As Integer) As Integer()
        Dim intRankCtr As Integer = 0
        Dim intRanks() As Integer
        Dim intRankLimit As Integer
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        Dim intByPlayerIDNum(8) As Integer
        Dim intByPlayerCount(8) As Integer
        Dim intByPlayerCtr As Integer = 0
        Dim intNdx2 As Integer

        Dim intThisPlayerID As Integer
        Dim intThisPlayerCount As Integer
        Dim intPlayerWithThisManyDice As Integer

        ReDim intRanks(8)

        ' Analyze dice on casino
        intLastNdx = gds.Tables("Dice").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("Dice").Rows(intNdx)
                If (.Item(0) = intCasinoID) Then
                    intThisPlayerID = .Item(1)
                    If (intThisPlayerID = -1) Then
                        Continue For
                    End If
                    If (.Item(3) = -1) Then
                        intThisPlayerID = 0
                    End If
                    intThisPlayerCount = -1
                    For intNdx2 = 1 To intByPlayerCtr
                        If (intByPlayerIDNum(intNdx2) = intThisPlayerID) Then
                            intThisPlayerCount = .Item(4)
                            intByPlayerCount(intNdx2) += intThisPlayerCount
                            Exit For
                        End If
                    Next
                    If (intThisPlayerCount = -1) Then
                        intByPlayerCtr += 1
                        intByPlayerIDNum(intByPlayerCtr) = intThisPlayerID
                        intByPlayerCount(intByPlayerCtr) = .Item(4)
                    End If
                End If
            End With
        Next
        '
        For intNdx = 8 To 0 Step -1
            intPlayerWithThisManyDice = -1
            For intNdx2 = 1 To intByPlayerCtr
                If (intByPlayerCount(intNdx2) = intNdx) Then
                    If (intPlayerWithThisManyDice <> -1) Then
                        intPlayerWithThisManyDice = -1
                        Exit For
                    End If
                    intPlayerWithThisManyDice = intByPlayerIDNum(intNdx2)
                End If
            Next
            If (intPlayerWithThisManyDice <> -1) Then
                intRankCtr += 1
                intRanks(intRankCtr) = intPlayerWithThisManyDice
            End If
        Next

        ' limit the array size to # of banknotes on casino
        intLastNdx = gds.Tables("Banknotes").Rows.Count - 1
        intRankLimit = 0
        For intNdx = 0 To intLastNdx
            With gds.Tables("Banknotes").Rows(intNdx)
                If (.Item(0) = intCasinoID) Then
                    intRankLimit += 1
                End If
            End With
        Next

        If (intRankCtr > intRankLimit) Then
            intRankCtr = intRankLimit
        End If
        ReDim Preserve intRanks(intRankCtr)
        Return intRanks

    End Function

    Sub ScoreTheRound() '

        Dim intNdx As Integer
        Dim intLastNdx As Integer
        Dim intRankings() As Integer
        Dim intNdx2 As Integer
        Dim intLastNdx2 As Integer
        Dim intNotes(8) As Integer
        Dim intNoteCnt As Integer

        AddToLog("End of round " & gintCurrRoundNum)
        intLastNdx = gds.Tables("BankNotes").Rows.Count - 1

        For intNdx = 1 To 6
            intNoteCnt = 0
            For intNdx2 = 0 To intLastNdx
                With gds.Tables("BankNotes").Rows(intNdx2)
                    If (.Item(0) = intNdx) Then
                        intNoteCnt += 1
                        intNotes(intNoteCnt) = .Item(1)
                    End If
                End With
            Next
            intRankings = CurrentScoreRankingsForCasino(intNdx)
            If (UBound(intRankings) > 0) Then
                intLastNdx2 = UBound(intRankings)
                For intNdx2 = 1 To intLastNdx2
                    AddToLog(GetPlayerNameFromID(intRankings(intNdx2)) & " gets $" & intNotes(intNdx2) & ",000")
                    AddDollarsToPlayer(intRankings(intNdx2), intNotes(intNdx2))
                Next
            End If
        Next
    End Sub

    Sub ScoreTheGame()
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        AddToLog("End of game")
        LoadNeededData()
        intLastNdx = gds.Tables("PlayerDetails").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("PlayerDetails").Rows(intNdx)
                AddToLog(.Item(3) & " final money $" & .Item(5) & ",000")
            End With
        Next
    End Sub
    Sub AddDollarsToPlayer(intThePlayerIDNum As Integer, intTheAmount As Integer)
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[AddDollarsToPlayer]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@PlayerNum", intThePlayerIDNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@Amount", intTheAmount))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            If (result <> 1) Then
                Throw New Exception("Unexpected return value from AddDollarsToPlayer")
            End If
        Catch ex As Exception
            Throw New Exception("AddDollarsToPlayer failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try
    End Sub

    Sub SetCasinoDisplay()
        Dim intCurrCasinoNum As Integer = 0
        Dim intThisCasinoNum As Integer
        Dim intThisDenomination As Integer
        Dim intRowNbr As Integer = -1
        Dim intLastRowNbr As Integer
        Dim intNdx As Integer
        Dim intLastNdx As Integer
        Dim intDiceCasinoNum As Integer
        Dim intDiceColor As Integer
        Dim intLastDiceColor As Integer
        Dim intDiceValue As Integer
        Dim intDiceCount As Integer
        Dim intDieNdx As Integer
        Dim intRankings() As Integer
        Dim strRankings As String

        Dim tr As TableRow
        Dim tc As TableCell
        Dim tc2 As TableCell

        tblDisplay.Rows.Clear()

        tblDisplay.BorderStyle = BorderStyle.Double
        tblDisplay.BorderWidth = New Unit(2, UnitType.Pixel)
        tblDisplay.CellPadding = 5
        tblDisplay.CellSpacing = 5
        tblDisplay.GridLines = GridLines.Both

        intLastRowNbr = gds.Tables("Banknotes").Rows.Count - 1
        intLastNdx = gds.Tables("Dice").Rows.Count - 1
        If (intLastRowNbr = -1) Then
            Throw New Exception("No data returned for game")
        Else
            ' repeatedly process denomonation rows for each casino
            Do While (True)
                intRowNbr += 1

                intThisCasinoNum = gds.Tables("Banknotes").Rows(intRowNbr).Item(0)
                intThisDenomination = gds.Tables("Banknotes").Rows(intRowNbr).Item(1)

                If (intThisCasinoNum <> intCurrCasinoNum) Then
                    If (intCurrCasinoNum <> 0) Then
                        ' end of previous casino, add the denom cell and then construct and add dice cell
                        tr.Cells.Add(tc)
                        tc = Nothing
                        ' add in rankings first
                        strRankings = ""
                        intRankings = CurrentScoreRankingsForCasino(intCurrCasinoNum)
                        If (UBound(intRankings) > 0) Then
                            For intNdx = 1 To UBound(intRankings)
                                If (strRankings <> "") Then
                                    strRankings &= ","
                                End If
                                strRankings &= GetPlayerNameFromID(intRankings(intNdx))
                            Next
                        End If
                        tc = New TableCell
                        tc.Text = strRankings
                        tr.Cells.Add(tc)
                        tc = Nothing
                        ' add in dice on casino, sentinel row with DieColor -999 ends the data
                        ' combine all dicecolor -1 (white) regardless of owner
                        tc = New TableCell
                        tc2 = Nothing
                        intLastDiceColor = 0
                        ' Process only rows for current casino
                        For intNdx = 0 To intLastNdx
                            With gds.Tables("Dice").Rows(intNdx)
                                intDiceCasinoNum = .Item(0)
                                If (intDiceCasinoNum = intCurrCasinoNum) Then
                                    '
                                    intDiceColor = .Item(3)
                                    If (intDiceColor <> intLastDiceColor) Then
                                        ' new dice color
                                        ' close off previous dice color if not closed off
                                        If (Not IsNothing(tc2)) Then
                                            tc.Controls.Add(tc2)
                                            tc2 = Nothing
                                        End If
                                        ' start new internal cell for new dice color unless sentinel
                                        If (intDiceColor <> -999) Then
                                            tc2 = New TableCell
                                            tc2.Text = ""
                                            tc2.BackColor = Drawing.Color.LightGray
                                            tc2.ForeColor = GS.GetColorMapping(intDiceColor)
                                        End If
                                        '
                                        intLastDiceColor = intDiceColor

                                    End If

                                    intDiceValue = .Item(2)
                                    intDiceCount = .Item(4)

                                    For intDieNdx = 1 To intDiceCount
                                        tc2.Text &= intDiceValue.ToString
                                    Next
                                End If
                            End With
                        Next

                        tr.Cells.Add(tc)
                        '
                        tblDisplay.Rows.Add(tr)
                        tr = Nothing
                    End If
                    'now set up new casino unless it is the sentinel

                    intCurrCasinoNum = intThisCasinoNum
                    If (intCurrCasinoNum = 999) Then
                        Exit Do
                    Else
                        tr = New TableRow
                        tc = New TableCell
                        tc.Text = intThisCasinoNum.ToString
                        tr.Cells.Add(tc)
                        tc = Nothing
                        tc = New TableCell
                        tc.Text = ""
                    End If
                End If
                ' add denomination
                tc.Text &= "$" & intThisDenomination.ToString & ",000" & "<BR>"
            Loop
        End If
    End Sub

    Sub SetUpAllPlayersDisplay()
        Dim intPlayerNdx As Integer
        Dim intLastNdx As Integer
        Dim intPlayerCnt As Integer
        Dim intThisPlayerNum As Integer

        Dim tr As TableRow
        Dim tc As TableCell

        intPlayerCnt = gds.Tables("PlayerDetails").Rows.Count
        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1

        tblPlayerInfo.BorderStyle = BorderStyle.Double
        tblPlayerInfo.BorderWidth = New Unit(2, UnitType.Pixel)
        'tblCurrPlayerDice.CellPadding = 5
        'tblCurrPlayerDice.CellSpacing = 5
        tblPlayerInfo.GridLines = GridLines.Both

        tblPlayerInfo.Rows.Clear()

        tr = New TableRow
        tc = New TableCell
        tc.Text = "Player Name"
        tc.Font.Bold = True
        tr.Cells.Add(tc)
        tc = Nothing
        tc = New TableCell
        tc.Text = "Winnings"
        tc.Font.Bold = True
        tr.Cells.Add(tc)
        tc = Nothing
        tc = New TableCell
        tc.Text = "Current Dice"
        tc.Font.Bold = True
        tr.Cells.Add(tc)
        tc = Nothing
        tblPlayerInfo.Rows.Add(tr)
        tr = Nothing

        For intPlayerNdx = 1 To intPlayerCnt
            intThisPlayerNum = GetPlayerIDByTurnSeqNum(intPlayerNdx)
            SetUpAltPlayerDisplay(intThisPlayerNum)
        Next

    End Sub
    Sub SetUpCurrentPlayerDisplay()

        Dim intNdx As Integer
        Dim intLastNdx As Integer
        Dim intDiceColor As Integer
        Dim intLastDiceColor As Integer
        Dim intDiceValue As Integer
        Dim intThisUserID As Integer
        Dim intOverride As Integer

        Dim boolCanPlaceOnCasino(6) As Boolean

        Dim tr As TableRow
        Dim tc As TableCell
        Dim tc2 As TableCell

        '' memory allocation for cells, will it get released?
        tblCurrPlayerDice.Rows.Clear()

        For intNdx = 1 To 6
            boolCanPlaceOnCasino(intNdx) = False
        Next
        ' fill in tblCurrPlayerDice

        tblCurrPlayerDice.BorderStyle = BorderStyle.Double
        tblCurrPlayerDice.BorderWidth = New Unit(2, UnitType.Pixel)
        'tblCurrPlayerDice.CellPadding = 5
        'tblCurrPlayerDice.CellSpacing = 5
        tblCurrPlayerDice.GridLines = GridLines.Horizontal

        tr = New TableRow
        tc = New TableCell

        tc2 = Nothing
        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1
        intLastDiceColor = 0
        ' Process only rows for current casino
        For intNdx = 0 To intLastNdx
            With gds.Tables("DiceInHands").Rows(intNdx)
                intThisUserID = .Item(0)
                If (intThisUserID <> gintPlayerNum) Then
                    Continue For
                End If
                If (.Item(4)) Then
                    Continue For
                End If
                intDiceColor = .Item(2)
                If (intDiceColor <> intLastDiceColor) Then
                    ' new dice color
                    ' close off previous dice color if not closed off
                    If (Not IsNothing(tc2)) Then
                        tc.Controls.Add(tc2)
                        tc2 = Nothing
                    End If
                    ' start new internal cell for new dice color unless sentinel
                    If (intDiceColor <> -999) Then
                        tc2 = New TableCell
                        tc2.Text = ""
                        tc2.BackColor = Drawing.Color.LightGray
                        tc2.ForeColor = GS.GetColorMapping(intDiceColor)
                    Else
                        Exit For
                    End If
                    '
                    intLastDiceColor = intDiceColor

                End If

                intDiceValue = .Item(1)
                boolCanPlaceOnCasino(intDiceValue) = True
                tc2.Text &= intDiceValue.ToString
            End With
        Next

        tr.Cells.Add(tc)
        '
        tblCurrPlayerDice.Rows.Add(tr)
        tr = Nothing

        intOverride = GetSelectedMove()

        ' fill in possible moves
        If (Not IsPostBack) Then
            rbLegalMoves.Items.Clear()
            For intNdx = 1 To 6
                If (boolCanPlaceOnCasino(intNdx)) Or (intOverride = intNdx) Then
                    Dim li As New ListItem
                    li.Text = "Play " & intNdx & "'s on Casino " & intNdx
                    li.Value = intNdx.ToString
                    If (intOverride = intNdx) Then
                        li.Selected = True
                    End If
                    rbLegalMoves.Items.Add(li)
                    li = Nothing
                End If
            Next
        End If
    End Sub

    Sub SetUpAltPlayerDisplay(intAltPlayerNum As Integer)
        Dim intNdx As Integer
        Dim intLastNdx As Integer
        Dim intDiceColor As Integer
        Dim intLastDiceColor As Integer
        Dim intDiceValue As Integer
        Dim intThisUserID As Integer

        Dim tr As TableRow
        Dim tc As TableCell
        Dim tc2 As TableCell

        Dim t2 As Table
        Dim tr2 As TableRow

        tr = New TableRow

        tc = New TableCell
        tc.Text = GetPlayerNameFromID(intAltPlayerNum)
        tc.ForeColor = GS.GetColorMapping(GetDiceColorFromID(intAltPlayerNum))
        'tc.BorderStyle = BorderStyle.Double
        'tc.BorderWidth = New Unit(2, UnitType.Pixel)
        tr.Cells.Add(tc)
        tc = Nothing

        tc = New TableCell
        tc.Text = "$" & GetPlayerMoneyFromID(intAltPlayerNum) & ",000"
        'tc.BorderStyle = BorderStyle.Double
        'tc.BorderWidth = New Unit(2, UnitType.Pixel)
        tr.Cells.Add(tc)
        tc = Nothing

        tc = New TableCell
        'tc.BorderStyle = BorderStyle.Double
        'tc.BorderWidth = New Unit(2, UnitType.Pixel)
        tc.BackColor = Drawing.Color.LightGray
        tc.Text = ""

        ' proper ? cell/table inside cell
        t2 = New Table
        t2.BorderStyle = BorderStyle.None
        t2.GridLines = GridLines.None
        tr2 = New TableRow

        tc2 = Nothing
        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1
        intLastDiceColor = 0

        For intNdx = 0 To intLastNdx
            With gds.Tables("DiceInHands").Rows(intNdx)
                intThisUserID = .Item(0)
                If (intThisUserID <> intAltPlayerNum) Then
                    Continue For
                End If
                If (.Item(4)) Then
                    Continue For
                End If
                intDiceColor = .Item(2)
                If (intDiceColor <> intLastDiceColor) Then
                    ' new dice color
                    ' close off previous dice color if not closed off
                    If (Not IsNothing(tc2)) Then
                        tr2.Cells.Add(tc2)
                        tc2 = Nothing
                    End If
                    ' start new internal cell for new dice color unless sentinel
                    If (intDiceColor <> -999) Then
                        tc2 = New TableCell
                        tc2.Text = ""
                        'tc2.BackColor = Drawing.Color.LightGray
                        tc2.ForeColor = GS.GetColorMapping(intDiceColor)
                    Else
                        Exit For
                    End If
                    '
                    intLastDiceColor = intDiceColor

                End If

                intDiceValue = .Item(1)
                tc2.Text &= intDiceValue.ToString
            End With
        Next
        t2.Rows.Add(tr2)
        tc.Controls.Add(t2)
        tr2 = Nothing
        t2 = Nothing
        tr.Cells.Add(tc)
        tc = Nothing
        '
        tblPlayerInfo.Rows.Add(tr)
        tr = Nothing

    End Sub

    Sub InitializePlayerDice()
        Dim intNdx As Integer
        Dim intColorToUse As Integer
        ' Set up all the initial dice, and flag as created
        ' Get the color of dice to use for current user
        intColorToUse = GetDiceColorFromID(gintPlayerNum)

        ' eight dice of player's own color
        For intNdx = 1 To 8
            CreateDieForUser(intColorToUse)
        Next
        ' two white dice
        For intNdx = 1 To 2
            CreateDieForUser(-1)
        Next
        '
        SetHasRolledStatus(False)
        '
    End Sub

    Sub CreateDieForUser(intColor As Integer)
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[AddDieToPlayer]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@PlayerNum", gintPlayerNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@ColorID", intColor))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            If (result <> 1) Then
                Throw New Exception("Unexpected return value from AddDieToPlayer")
            End If
        Catch ex As Exception
            Throw New Exception("AddDieToPlayer failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try
    End Sub
    Sub RollPlayerDice()
        Dim intNdx As Integer
        Dim intLastNdx As Integer
        Dim intDieID As Integer
        Dim intNewValue As Integer
        Dim R As New Random

        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("DiceInHands").Rows(intNdx)
                If (.Item(0) = gintPlayerNum) Then
                    intDieID = .Item(3)
                    If (intDieID <> 999) Then
                        intNewValue = R.Next(1, 6)
                        SetDieToValue(intDieID, intNewValue)
                    End If
                End If
            End With
        Next

        SetHasRolledStatus(True)
    End Sub

    Sub SetDieToValue(intDieSeqNum As Integer, intValue As Integer)
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[SetDieValue]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@UserIDNum", gintPlayerNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@DieSeqNum", intDieSeqNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@DieValue", intValue))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            If (result <> 1) Then
                Throw New Exception("Unexpected return value from SetDieToValue")
            End If
        Catch ex As Exception
            Throw New Exception("SetDieToValue failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try
    End Sub
    Sub SetHasRolledStatus(hasRolled As Boolean)
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[SetHasRolledValue]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@UserIDNum", gintPlayerNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@hasRolled", hasRolled))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            If (result <> 1) Then
                Throw New Exception("Unexpected return value from SetHasRolledStatus")
            End If
        Catch ex As Exception
            Throw New Exception("SetHasRolledStatus failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try
    End Sub

    Sub SetGameState(intOpCode As Integer, intP1 As Integer)
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[SetGameState]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@OpCode", intOpCode))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@Param1", intP1))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            If (result <> 1) Then
                Throw New Exception("Unexpected return value from SetGameState")
            End If
        Catch ex As Exception
            Throw New Exception("SetGameState failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try
    End Sub
    '---
    Sub AddToLog(strMessage As String)
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Try
            DBcon.ConnectionString = CS.MainConnection
            DBcon.Open()
            DBcmd.Connection = DBcon
            DBcmd.CommandType = CommandType.StoredProcedure
            DBcmd.CommandText = "[Dice].[AddToLog]"
            DBcmd.Parameters.Clear()
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameNum))
            DBcmd.Parameters.Add(New SqlClient.SqlParameter("@MesssageText", strMessage))
            Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
            intRetParam.Direction = ParameterDirection.ReturnValue
            DBcmd.Parameters.Add(intRetParam)
            DBcmd.ExecuteNonQuery()
            Dim result As Integer
            result = DBcmd.Parameters("RETURN_VALUE").Value
            If (result <> 1) Then
                Throw New Exception("Unexpected return value from AddToLog")
            End If
        Catch ex As Exception
            Throw New Exception("AddToLog failed", ex)
        Finally
            If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
        End Try
    End Sub
    Function PosessiveOf(strName As String) As String
        Dim strLastChar As String

        strLastChar = strName.Substring(strName.Length - 1, 1).ToUpper
        If (strLastChar = "S") Then
            Return strName & "'"
        Else
            Return strName & "'s"
        End If
    End Function


    Function GetPlayerNameFromID(intPlayerNum As Integer) As String
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        If (intPlayerNum = 0) Then
            Return "House"
        End If
        intLastNdx = gds.Tables("PlayerDetails").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("PlayerDetails").Rows(intNdx)
                If (.Item(0) = intPlayerNum) Then
                    Return .Item(3).ToString
                End If
            End With
        Next

        Throw New Exception("Get Player Name failed")
    End Function

    Function GetPlayerMoneyFromID(intPlayerNum As Integer) As String
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        intLastNdx = gds.Tables("PlayerDetails").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("PlayerDetails").Rows(intNdx)
                If (.Item(0) = intPlayerNum) Then
                    Return .Item(5).ToString
                End If
            End With
        Next

        Throw New Exception("GetPlayerMoneyFromID failed")
    End Function

    Function GetPlayerDiceColorID(intPlayerNum As Integer) As Integer
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        intLastNdx = gds.Tables("PlayerDetails").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("PlayerDetails").Rows(intNdx)
                If (.Item(0) = intPlayerNum) Then
                    Return .Item(2)
                End If
            End With
        Next

        Throw New Exception("GetPlayerDiceColorID failed")
    End Function

    Function GetPlayerIDByTurnSeqNum(intTurnSeqNum As Integer) As Integer
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        intLastNdx = gds.Tables("PlayerDetails").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("PlayerDetails").Rows(intNdx)
                If (.Item(1) = intTurnSeqNum) Then
                    Return .Item(0)
                End If
            End With
        Next

        Throw New Exception("Get Player Name failed")
    End Function

    Function GetSelectedMove() As Integer
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        intLastNdx = gds.Tables("DiceInHands").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("DiceInHands").Rows(intNdx)
                If (.Item(0) = gintPlayerNum) Then
                    '' Microsoft docs say SQL BIT maps to .NET Boolean directly
                    If (.Item(4)) Then
                        Return .Item(1)
                    End If
                End If
            End With
        Next

        Return 0
    End Function
    Function GetHasRolledStatusForPlayerID(intPlayerNum As Integer) As eRollStatus
        ' Based on current meta data loaded ...
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        intLastNdx = gds.Tables("PlayerDetails").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("PlayerDetails").Rows(intNdx)
                If (.Item(0) = intPlayerNum) Then
                    '' Microsoft docs say SQL BIT maps to .NET Boolean directly
                    If (IsDBNull(.Item(4))) Then
                        Return eRollStatus.HasNotRolledEver
                    ElseIf (.Item(4)) Then
                        Return eRollStatus.HasRolledForThisTurn
                    Else
                        Return eRollStatus.HasNotRolledForThisTurn
                    End If
                End If
            End With
        Next

        Throw New Exception("GetHasRolledStatusForPlayerID failed")

    End Function
    Function GetDiceColorFromID(intPlayerNum As Integer) As Integer
        Dim intNdx As Integer
        Dim intLastNdx As Integer

        intLastNdx = gds.Tables("PlayerDetails").Rows.Count - 1
        For intNdx = 0 To intLastNdx
            With gds.Tables("PlayerDetails").Rows(intNdx)
                If (.Item(0) = intPlayerNum) Then
                    Return .Item(2)
                End If
            End With
        Next

        Throw New Exception("DiceColorFromID failed")
    End Function
    ' ---
    Function GetFromSession(strVariableName As String) As String
        Dim strValue As String = ""

        If (Not IsNothing(Session(strVariableName))) Then '
            strValue = Session(strVariableName)
        End If

        Return strValue
    End Function


End Class