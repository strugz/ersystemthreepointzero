Imports System.Data.Entity
Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class TransactionRepository
        Implements ITransactionRepository

        Public Function GetFWMSBySRNumber(srNumber As String) As FwmsTransactionDto Implements ITransactionRepository.GetFWMSBySRNumber
            If String.IsNullOrWhiteSpace(srNumber) Then
                Return Nothing
            End If

            Dim normalizedSrNumber As String = srNumber.Trim()

            Using dbContext As New FwmsDbContext()
                Dim query = From trade In dbContext.TradeMasters.AsNoTracking()
                            Join contact In dbContext.ContactMasters.AsNoTracking() On trade.TRDMUI Equals contact.CNTMID
                            Join account In dbContext.AccountMasters.AsNoTracking() On trade.TRDMAC Equals account.ACCMID
                            Where trade.TRDMTT = normalizedSrNumber And trade.TRDMTT.Contains("FLD")
                            Select New FwmsTransactionDto With {
                                .ACCMSC = account.ACCMSC,
                                .ACCMNM = account.ACCMNM,
                                .CNTMNN = contact.CNTMNN,
                                .TRDMTY = trade.TRDMTY,
                                .TRDMMC = trade.TRDMMC,
                                .TRDMDE = trade.TRDMDE,
                                .TRDMTT = trade.TRDMTT,
                                .TRDSEC = trade.TRDSEC,
                                .TRDSTS = trade.TRDSTS,
                                .TRDMCD = trade.TRDMCD
                            }

                Return query.FirstOrDefault()
            End Using
        End Function

        Public Function GetFWMSByTransactionDateRange(dateFrom As Date, dateTo As Date, userInitial As String) As List(Of FwmsTransactionDto) Implements ITransactionRepository.GetFWMSByTransactionDateRange
            Dim startDate As Date = dateFrom.Date
            Dim endDateExclusive As Date = dateTo.Date.AddDays(1)
            Dim normalizedUserInitial As String = If(userInitial, String.Empty).Trim()

            If endDateExclusive <= startDate OrElse normalizedUserInitial.Length = 0 Then
                Return New List(Of FwmsTransactionDto)()
            End If

            Using dbContext As New FwmsDbContext()
                Dim query = From trade In dbContext.TradeMasters.AsNoTracking()
                            Join contact In dbContext.ContactMasters.AsNoTracking() On trade.TRDMUI Equals contact.CNTMID
                            Join account In dbContext.AccountMasters.AsNoTracking() On trade.TRDMAC Equals account.ACCMID
                            Where trade.TRDMCD.HasValue AndAlso
                                trade.TRDMCD.Value >= startDate AndAlso
                                trade.TRDMCD.Value < endDateExclusive AndAlso
                                contact.CNTMNN = normalizedUserInitial AndAlso
                                trade.TRDMTT.Contains("FLD")
                            Order By trade.TRDMCD Descending, account.ACCMNM, trade.TRDMTT
                            Select New FwmsTransactionDto With {
                                .ACCMSC = account.ACCMSC,
                                .ACCMNM = account.ACCMNM,
                                .CNTMNN = contact.CNTMNN,
                                .TRDMTY = trade.TRDMTY,
                                .TRDMMC = trade.TRDMMC,
                                .TRDMDE = trade.TRDMDE,
                                .TRDMTT = trade.TRDMTT,
                                .TRDSEC = trade.TRDSEC,
                                .TRDSTS = trade.TRDSTS,
                                .TRDMCD = trade.TRDMCD
                            }

                Return query.ToList()
            End Using
        End Function
    End Class
End Namespace
