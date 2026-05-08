Option Strict On

Imports ER_System.Presentation.ViewModels

Namespace Presentation.Presenters
    Public Class ReportPrintPresenter
        Public Function IsAdminOwner(ByVal model As ReportPrintViewModel) As Boolean
            If model Is Nothing Then
                Return False
            End If

            Return String.Equals(model.UserLevel, "Admin", StringComparison.OrdinalIgnoreCase) AndAlso
                String.Equals(model.ReportOwnerUserId, model.CurrentUserId, StringComparison.OrdinalIgnoreCase)
        End Function

        Public Function CanSendToPrint(ByVal reportId As String) As String
            If String.IsNullOrWhiteSpace(reportId) Then
                Return "Select Report To Send"
            End If

            Return String.Empty
        End Function

        Public Function BuildExportFilePath(ByVal model As ReportPrintViewModel, ByVal startupPath As String) As String
            If model Is Nothing Then
                Return String.Empty
            End If

            Dim datePart As String = model.ReportDate.ToString("ddMMMyyyy").ToUpperInvariant()

            If model.Rbt = "0" Then
                Return startupPath & "\ERPDF\" & model.Username & "ER" & datePart & ".pdf"
            End If

            Return startupPath & "\ERPDF\" & model.Username & model.LocationCode & datePart & ".pdf"
        End Function
    End Class
End Namespace
