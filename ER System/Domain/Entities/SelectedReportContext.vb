Option Strict On

Namespace Domain.Entities
    Friend Class SelectedReportContext
        Public Const ReportDescriptionIndex As Integer = 0
        Public Const ReportDateFromIndex As Integer = 1
        Public Const ReportDateToIndex As Integer = 2
        Public Const ReportPrintStatusIndex As Integer = 3
        Public Const ReportFileStatusIndex As Integer = 4
        Public Const ReportSentStatusIndex As Integer = 5
        Public Const CashCheckIndex As Integer = 6
        Public Const CashDateIndex As Integer = 7
        Public Const CashRefDocIndex As Integer = 8
        Public Const CashRefNoIndex As Integer = 9
        Public Const CashAmountIndex As Integer = 10
        Public Const RevolvingFundIndex As Integer = 11
        Public Const StatusIndex As Integer = 12
        Public Const ReportIdIndex As Integer = 13
        Public Const UserIdIndex As Integer = 14

        Public Property Values As String() = Array.Empty(Of String)()

        Public ReadOnly Property HasSelection As Boolean
            Get
                Return Values IsNot Nothing AndAlso Values.Length > 13 AndAlso Not String.IsNullOrWhiteSpace(ReportId)
            End Get
        End Property

        Public ReadOnly Property Description As String
            Get
                Return GetValue(ReportDescriptionIndex)
            End Get
        End Property

        Public ReadOnly Property ReportDateFrom As String
            Get
                Return GetValue(ReportDateFromIndex)
            End Get
        End Property

        Public ReadOnly Property ReportDateTo As String
            Get
                Return GetValue(ReportDateToIndex)
            End Get
        End Property

        Public ReadOnly Property PrintStatus As String
            Get
                Return GetValue(ReportPrintStatusIndex)
            End Get
        End Property

        Public ReadOnly Property ReportFileStatus As String
            Get
                Return GetValue(ReportFileStatusIndex)
            End Get
        End Property

        Public ReadOnly Property ReportSentStatus As String
            Get
                Return GetValue(ReportSentStatusIndex)
            End Get
        End Property

        Public ReadOnly Property CashCheck As String
            Get
                Return GetValue(CashCheckIndex)
            End Get
        End Property

        Public ReadOnly Property CashDate As String
            Get
                Return GetValue(CashDateIndex)
            End Get
        End Property

        Public ReadOnly Property CashRefDoc As String
            Get
                Return GetValue(CashRefDocIndex)
            End Get
        End Property

        Public ReadOnly Property CashRefNo As String
            Get
                Return GetValue(CashRefNoIndex)
            End Get
        End Property

        Public ReadOnly Property CashAmount As String
            Get
                Return GetValue(CashAmountIndex)
            End Get
        End Property

        Public ReadOnly Property RevolvingFund As String
            Get
                Return GetValue(RevolvingFundIndex)
            End Get
        End Property

        Public ReadOnly Property Status As String
            Get
                Return GetValue(StatusIndex)
            End Get
        End Property

        Public ReadOnly Property ReportId As String
            Get
                Return GetValue(ReportIdIndex)
            End Get
        End Property

        Public ReadOnly Property UserId As String
            Get
                Return GetValue(UserIdIndex)
            End Get
        End Property

        Private Function GetValue(index As Integer) As String
            If Values Is Nothing OrElse index < 0 OrElse index >= Values.Length Then
                Return String.Empty
            End If

            Return If(Values(index), String.Empty)
        End Function
    End Class
End Namespace
