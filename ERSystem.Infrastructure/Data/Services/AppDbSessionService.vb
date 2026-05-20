Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class AppDbSessionService
        Implements IAppDbSessionService

        Public Sub StartAfterLoginSuccess() Implements IAppDbSessionService.StartAfterLoginSuccess
            AppDbOptimisticPreloader.Start()
        End Sub

        Public Sub ClearOnLogoutOrExit() Implements IAppDbSessionService.ClearOnLogoutOrExit
            AppDbSessionCache.Clear()
        End Sub
    End Class
End Namespace
