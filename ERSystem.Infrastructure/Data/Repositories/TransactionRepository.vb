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

            Using dbContext As New AppDbContext()
                Dim query = From trade In dbContext.TradeMasters.AsNoTracking()
                            Join contact In dbContext.ContactMasters.AsNoTracking() On trade.TRDMUI Equals contact.CNTMID
                            Join account In dbContext.AccountMasters.AsNoTracking() On trade.TRDMAC Equals account.ACCMID
                            Where trade.TRDMTT = normalizedSrNumber
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
    End Class
End Namespace
