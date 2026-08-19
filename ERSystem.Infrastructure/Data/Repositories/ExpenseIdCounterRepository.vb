Option Strict On

Imports System.Data.Entity.Migrations
Imports System.Globalization
Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ExpenseIdCounterRepository
        Implements IExpenseIdCounterRepository

        Private Const ExpenseDetailCounterName As String = "ExpenseDetail"
        Private Const ExpenseIdSequenceDigits As Integer = 4
        Private Const ExpenseIdSequenceLimit As Integer = 9999

        Public Function GetNextExpenseId(dbContext As AppDbContext) As Long Implements IExpenseIdCounterRepository.GetNextExpenseId
            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim yearMonthDate As String = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            Dim counterName As String = CreateCounterName(yearMonthDate)
            Dim counter = dbContext.Database.SqlQuery(Of ExpenseIdCounterModel)(
                "SELECT * FROM tbExpenseIdCounter WITH (UPDLOCK, HOLDLOCK) WHERE CounterName = @p0",
                counterName).FirstOrDefault()

            If counter Is Nothing Then
                counter = New ExpenseIdCounterModel With {
                    .CounterName = counterName,
                    .CurrentValue = 1,
                    .UpdatedAt = DateTime.UtcNow
                }
                dbContext.ExpenseIdCounters.Add(counter)
            Else
                counter.CurrentValue += 1
                counter.UpdatedAt = DateTime.UtcNow
                dbContext.ExpenseIdCounters.AddOrUpdate(counter)
            End If

            If counter.CurrentValue > ExpenseIdSequenceLimit Then
                Throw New InvalidOperationException("Monthly expense ID sequence exceeded 9999.")
            End If

            dbContext.SaveChanges()
            Return CreateExpenseId(yearMonthDate, counter.CurrentValue)
        End Function

        Private Shared Function CreateCounterName(yearMonth As String) As String
            Return String.Format(CultureInfo.InvariantCulture, "{0}:{1}", ExpenseDetailCounterName, yearMonth)
        End Function

        Private Shared Function CreateExpenseId(yearMonth As String, sequence As Long) As Long
            Dim generatedId As Long = Long.Parse(
                String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}",
                    yearMonth,
                    sequence.ToString(New String("0"c, ExpenseIdSequenceDigits), CultureInfo.InvariantCulture)),
                CultureInfo.InvariantCulture)

            If generatedId > Long.MaxValue Then
                Throw New InvalidOperationException("Generated expense ID exceeds the tbExpenseDetails.ID integer range.")
            End If

            Return CLng(generatedId)
        End Function
    End Class
End Namespace
