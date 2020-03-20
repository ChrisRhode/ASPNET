Public Class SharedRoutines

    Private gintGameID As Integer
    Public Sub New(intCurrGameID As Integer)
        gintGameID = intCurrGameID
    End Sub
    Public Sub SetUpCasinos()
        Dim DBcon As New SqlClient.SqlConnection
        Dim DBcmd As New SqlClient.SqlCommand
        Dim CS As New cConnections

        Dim amounts(54) As Integer
        Dim amountsCtr As Integer = 0
        Dim intNdx As Integer
        Dim R As New Random
        Dim intSlotNbr As Integer
        Dim intSumSoFar As Integer
        Dim intNextAmountNdx As Integer
        Dim intNextAmount As Integer

        For intNdx = 1 To 5
            AddToAmounts(amounts, amountsCtr, 90)
            AddToAmounts(amounts, amountsCtr, 80)
            AddToAmounts(amounts, amountsCtr, 70)
            AddToAmounts(amounts, amountsCtr, 60)
        Next
        For intNdx = 1 To 6
            AddToAmounts(amounts, amountsCtr, 50)
            AddToAmounts(amounts, amountsCtr, 40)
            AddToAmounts(amounts, amountsCtr, 10)
        Next
        For intNdx = 1 To 8
            AddToAmounts(amounts, amountsCtr, 30)
            AddToAmounts(amounts, amountsCtr, 20)
        Next

        intSlotNbr = 0
        intSumSoFar = 50

        Do While (True)
            If (intSumSoFar >= 50) Then
                intSumSoFar = 0
                intSlotNbr += 1
                If (intSlotNbr > 6) Then
                    Exit Do
                End If
            End If

            intNextAmountNdx = R.Next(1, amountsCtr)
            intNextAmount = amounts(intNextAmountNdx)

            Try
                DBcon.ConnectionString = CS.MainConnection
                DBcon.Open()
                DBcmd.Connection = DBcon
                DBcmd.CommandType = CommandType.StoredProcedure
                DBcmd.CommandText = "[Dice].[AddBankNoteToCasino]"
                DBcmd.Parameters.Clear()
                DBcmd.Parameters.Add(New SqlClient.SqlParameter("@GameNum", gintGameID))
                DBcmd.Parameters.Add(New SqlClient.SqlParameter("@CasinoNum", intSlotNbr))
                DBcmd.Parameters.Add(New SqlClient.SqlParameter("@Denomination", intNextAmount))
                Dim intRetParam As New SqlClient.SqlParameter("RETURN_VALUE", SqlDbType.Int)
                intRetParam.Direction = ParameterDirection.ReturnValue
                DBcmd.Parameters.Add(intRetParam)
                DBcmd.ExecuteNonQuery()
                Dim result As Integer
                result = DBcmd.Parameters("RETURN_VALUE").Value
                If (result <> 1) Then
                    Throw New Exception("Unexpected return value from Add Bank Note to Casino")
                End If
            Catch ex As Exception
                Throw New Exception("Add New Bank Note to Casino failed", ex)
            Finally
                If (DBcon.State <> ConnectionState.Closed) Then DBcon.Close()
            End Try

            RemoveFromAmounts(amounts, amountsCtr, intNextAmountNdx)
            intSumSoFar += intNextAmount
        Loop

    End Sub

    Private Sub AddToAmounts(ByRef amounts() As Integer, ByRef counter As Integer, thisAmount As Integer)
        counter += 1
        amounts(counter) = thisAmount
    End Sub

    Private Sub RemoveFromAmounts(ByRef amounts() As Integer, ByRef counter As Integer, indexToRemove As Integer)
        Dim intNdx As Integer

        If (indexToRemove <> counter) Then
            For intNdx = indexToRemove + 1 To counter
                amounts(intNdx - 1) = amounts(intNdx)
            Next
        End If
        counter -= 1
    End Sub

    Public Function MaxUserColorNum() As Integer
        Return 9
    End Function
    Public Function GetColorMapping(intColorNum As Integer) As Drawing.Color
        Select Case intColorNum
            Case 1
                Return Drawing.Color.Red
            Case 2
                Return Drawing.Color.Green
            Case 3
                Return Drawing.Color.Blue
            Case 4
                Return Drawing.Color.Black
            Case 5
                Return Drawing.Color.HotPink
            Case 6
                Return Drawing.Color.Brown
            Case 7
                Return Drawing.Color.Cyan
            Case 8
                Return Drawing.Color.Magenta
            Case 9
                Return Drawing.Color.Yellow
            Case -1
                Return Drawing.Color.White
            Case -2
                Return Drawing.Color.Purple
            Case Else
                Throw New Exception("Bad color ID")
        End Select
    End Function

End Class
