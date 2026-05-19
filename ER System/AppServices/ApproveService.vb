Option Strict On

Namespace AppServices
    Friend Class ApproveService
        Private ReadOnly _approveRepository As Infrastructure.Data.Repositories.ApproveRepository
        Private ReadOnly _settingsRegistryProvider As Infrastructure.Configuration.SettingsRegistryProvider
        Private ReadOnly _userAccountRegistryProvider As Infrastructure.Configuration.UserAccountRegistryProvider

        Public Sub New()
            _approveRepository = New Infrastructure.Data.Repositories.ApproveRepository()
            _settingsRegistryProvider = New Infrastructure.Configuration.SettingsRegistryProvider()
            _userAccountRegistryProvider = New Infrastructure.Configuration.UserAccountRegistryProvider()
        End Sub

        Public Function LoadUserAccounts() As ApproveUserAccountLoadResult
            Dim deptId As String = _userAccountRegistryProvider.GetValue("DeptID")
            Dim signId As String = _userAccountRegistryProvider.GetValue("UserID")
            Dim changeLoading As String = _settingsRegistryProvider.GetValue("ChangeLoading")
            Dim users As DataTable = _approveRepository.LoadUserAccountFiled(deptId, signId)

            Return New ApproveUserAccountLoadResult With {
                .Users = users,
                .ChangeLoading = changeLoading,
                .ShowNumberOfFile = Not String.Equals(changeLoading, "0", StringComparison.Ordinal)
            }
        End Function

        Public Function LoadReportDetails(userId As String) As ApproveReportDetailsLoadResult
            Dim changeLoading As String = _settingsRegistryProvider.GetValue("ChangeLoading")
            Dim signId As String = _userAccountRegistryProvider.GetValue("UserID")
            Dim reportDetails As DataTable

            If String.Equals(changeLoading, "1", StringComparison.Ordinal) Then
                reportDetails = _approveRepository.LoadUserReportDetailsFiled(userId, changeLoading, signId)
            Else
                reportDetails = _approveRepository.LoadUserReportDetailsDone(userId, changeLoading, signId)
            End If

            Return New ApproveReportDetailsLoadResult With {
                .ReportDetails = reportDetails,
                .HasRows = reportDetails IsNot Nothing AndAlso reportDetails.Rows.Count <> 0
            }
        End Function

        Public Function ReloadAfterReject(reportUserId As String) As ApproveReloadResult
            Dim changeLoading As String = _settingsRegistryProvider.GetValue("ChangeLoading")
            Dim userAccountsResult As ApproveUserAccountLoadResult = LoadUserAccounts()
            Dim reportDetailsResult As ApproveReportDetailsLoadResult = LoadReportDetails(reportUserId)

            If String.Equals(changeLoading, "1", StringComparison.Ordinal) Then
                _settingsRegistryProvider.Save({"ChangeLoading"}, {"1"})
            Else
                _settingsRegistryProvider.Save({"ChangeLoading"}, {"0"})
            End If

            Return New ApproveReloadResult With {
                .UserAccounts = userAccountsResult.Users,
                .ReportDetails = reportDetailsResult.ReportDetails,
                .ShowNumberOfFile = userAccountsResult.ShowNumberOfFile,
                .ChangeLoading = changeLoading
            }
        End Function
    End Class
End Namespace
