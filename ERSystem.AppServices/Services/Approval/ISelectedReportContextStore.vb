Option Strict On

Public Interface ISelectedReportContextStore
    Sub Save(reportId As String)
    Function LoadValues() As String()
End Interface
