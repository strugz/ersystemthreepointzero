Imports System.Data
Imports System.Data.SqlClient
Imports ERSystem.Core.Domain.Entities
Imports ERSystem.Core.Domain.Interfaces

Namespace Repositories
    Public Class SqlExpenseRepository
        Implements IExpenseRepository

        Private ReadOnly _connectionString As String

        Public Sub New(connectionString As String)
            _connectionString = connectionString
        End Sub

        Public Sub AddExpense(expense As Expense) Implements IExpenseRepository.AddExpense
            Dim mealValues() As String = GetExpenseParts(expense.UserExpenseMeal)
            Dim transValues() As String = GetExpenseParts(expense.UserExpenseTransportation)

            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_AddExpense]", conn)
                    cmd.CommandType = CommandType.StoredProcedure

                    cmd.Parameters.Add("@transdate", SqlDbType.VarChar).Value = expense.TransDate
                    cmd.Parameters.Add("@perdiem", SqlDbType.VarChar).Value = expense.PerDiem
                    cmd.Parameters.Add("@particulars", SqlDbType.VarChar).Value = expense.Particulars
                    cmd.Parameters.Add("@invoice", SqlDbType.VarChar).Value = expense.Invoice
                    cmd.Parameters.Add("@multiplier", SqlDbType.VarChar).Value = expense.Multiplier
                    cmd.Parameters.Add("@type", SqlDbType.VarChar).Value = expense.ExtType
                    cmd.Parameters.Add("@category", SqlDbType.VarChar).Value = expense.Category
                    cmd.Parameters.Add("@amount", SqlDbType.VarChar).Value = expense.Amount
                    cmd.Parameters.Add("@remarks", SqlDbType.VarChar).Value = expense.Remarks
                    cmd.Parameters.Add("@status", SqlDbType.VarChar).Value = expense.Status
                    cmd.Parameters.Add("@totalamount", SqlDbType.VarChar).Value = expense.TotalAmount
                    cmd.Parameters.Add("@location", SqlDbType.VarChar).Value = expense.Location
                    cmd.Parameters.Add("@userid", SqlDbType.VarChar).Value = expense.UserID
                    cmd.Parameters.Add("@reportID", SqlDbType.VarChar).Value = expense.ReportID
                    cmd.Parameters.Add("@workWith", SqlDbType.VarChar).Value = expense.WorkWith
                    cmd.Parameters.Add("@serviceNumber", SqlDbType.VarChar).Value = expense.ServiceNumber
                    cmd.Parameters.Add("@instrument", SqlDbType.VarChar).Value = expense.Instrument
                    cmd.Parameters.Add("@serialNumber", SqlDbType.VarChar).Value = expense.SerialNumber
                    cmd.Parameters.Add("@mdays", SqlDbType.VarChar).Value = expense.MDays
                    cmd.Parameters.Add("@computation", SqlDbType.VarChar).Value = expense.Computation
                    cmd.Parameters.Add("@totdays", SqlDbType.VarChar).Value = expense.TotDays

                    cmd.Parameters.Add("@meal1", SqlDbType.VarChar).Value = mealValues(0)
                    cmd.Parameters.Add("@meal2", SqlDbType.VarChar).Value = mealValues(1)
                    cmd.Parameters.Add("@meal3", SqlDbType.VarChar).Value = mealValues(2)

                    cmd.Parameters.Add("@trans1", SqlDbType.VarChar).Value = transValues(0)
                    cmd.Parameters.Add("@trans2", SqlDbType.VarChar).Value = transValues(1)
                    cmd.Parameters.Add("@trans3", SqlDbType.VarChar).Value = transValues(2)

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Public Sub UpdateExpense(expense As Expense) Implements IExpenseRepository.UpdateExpense
            Dim mealValues() As String = GetExpenseParts(expense.UserExpenseMeal)
            Dim transValues() As String = GetExpenseParts(expense.UserExpenseTransportation)

            Using conn As New SqlConnection(_connectionString)
                Using cmd As New SqlCommand("[sp2_updateExpense]", conn)
                    cmd.CommandType = CommandType.StoredProcedure

                    cmd.Parameters.Add("@transID", SqlDbType.VarChar).Value = expense.TransID
                    cmd.Parameters.Add("@transdate", SqlDbType.VarChar).Value = expense.TransDate
                    cmd.Parameters.Add("@perdiem", SqlDbType.VarChar).Value = expense.PerDiem
                    cmd.Parameters.Add("@particulars", SqlDbType.VarChar).Value = expense.Particulars
                    cmd.Parameters.Add("@invoice", SqlDbType.VarChar).Value = expense.Invoice
                    cmd.Parameters.Add("@multiplier", SqlDbType.VarChar).Value = expense.Multiplier
                    cmd.Parameters.Add("@type", SqlDbType.VarChar).Value = expense.ExtType
                    cmd.Parameters.Add("@category", SqlDbType.VarChar).Value = expense.Category
                    cmd.Parameters.Add("@amount", SqlDbType.VarChar).Value = expense.Amount
                    cmd.Parameters.Add("@remarks", SqlDbType.VarChar).Value = expense.Remarks
                    cmd.Parameters.Add("@status", SqlDbType.VarChar).Value = expense.Status
                    cmd.Parameters.Add("@totalamount", SqlDbType.VarChar).Value = expense.TotalAmount
                    cmd.Parameters.Add("@location", SqlDbType.VarChar).Value = expense.Location
                    cmd.Parameters.Add("@userid", SqlDbType.VarChar).Value = expense.UserID
                    cmd.Parameters.Add("@workWith", SqlDbType.VarChar).Value = expense.WorkWith
                    cmd.Parameters.Add("@serviceNumber", SqlDbType.VarChar).Value = expense.ServiceNumber
                    cmd.Parameters.Add("@instrument", SqlDbType.VarChar).Value = expense.Instrument
                    cmd.Parameters.Add("@serialNumber", SqlDbType.VarChar).Value = expense.SerialNumber
                    cmd.Parameters.Add("@mdays", SqlDbType.VarChar).Value = expense.MDays
                    cmd.Parameters.Add("@computation", SqlDbType.VarChar).Value = expense.Computation
                    cmd.Parameters.Add("@totdays", SqlDbType.VarChar).Value = expense.TotDays

                    cmd.Parameters.Add("@meal1", SqlDbType.VarChar).Value = mealValues(0)
                    cmd.Parameters.Add("@meal2", SqlDbType.VarChar).Value = mealValues(1)
                    cmd.Parameters.Add("@meal3", SqlDbType.VarChar).Value = mealValues(2)

                    cmd.Parameters.Add("@trans1", SqlDbType.VarChar).Value = transValues(0)
                    cmd.Parameters.Add("@trans2", SqlDbType.VarChar).Value = transValues(1)
                    cmd.Parameters.Add("@trans3", SqlDbType.VarChar).Value = transValues(2)

                    conn.Open()
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        End Sub

        Private Function GetExpenseParts(ByVal rawValue As String) As String()
            Dim values() As String = {"", "", ""}
            If String.IsNullOrEmpty(rawValue) Then
                Return values
            End If

            Dim parts() As String = rawValue.Split("/"c)
            For i As Integer = 0 To Math.Min(parts.Length, 3) - 1
                values(i) = parts(i)
            Next

            Return values
        End Function

    End Class
End Namespace
