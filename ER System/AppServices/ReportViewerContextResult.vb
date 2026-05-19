Option Strict On

Imports CrystalDecisions.CrystalReports.Engine

Namespace AppServices
    Friend Class ReportViewerContextResult
        Public Property HasSelection As Boolean
        Public Property ReportId As String = String.Empty
        Public Property ReportUserId As String = String.Empty
        Public Property Status As String = String.Empty
        Public Property PrintStatus As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property IsAdminViewingOwnReport As Boolean
        Public Property CanSendPrint As Boolean
        Public Property ViewerReport As ReportDocument
    End Class
End Namespace
