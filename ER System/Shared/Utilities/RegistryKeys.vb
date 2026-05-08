Option Strict On

''' <summary>
''' Centralizes all Windows Registry path and key name constants used across the application.
''' Use these instead of inline magic strings to prevent typos and simplify future changes.
''' </summary>
Public NotInheritable Class RegistryKeys

    ' ── Base paths ──────────────────────────────────────────────────────────
    Public Const ConnectionPath As String = "Software\\ER System\\Connection"
    Public Const UserAccountPath As String = "Software\\ER System\\UserAccount"
    Public Const SettingsPath As String = "Software\\ER System\\Settings"

    ' ── Connection keys ─────────────────────────────────────────────────────
    Public Const DBType As String = "DBType"
    Public Const Authentication As String = "Authentication"
    Public Const ServerName As String = "ServerName"
    Public Const DatabaseName As String = "Database"
    Public Const UserName As String = "UserName"
    Public Const Password As String = "Password"
    Public Const ERUpdater As String = "ERUpdater"

    ' ── UserAccount keys ────────────────────────────────────────────────────
    Public Const UserID As String = "UserID"
    Public Const UsernameKey As String = "username"
    Public Const DeptID As String = "DeptID"
    Public Const BreakFastRate As String = "BreakFastRate"
    Public Const LunchRate As String = "LunchRate"
    Public Const DinnerRate As String = "DinnerRate"
    Public Const OTMeal As String = "OTMeal"
    Public Const TotalDays As String = "TotalDays"
    Public Const TranspoRate As String = "TranspoRate"

    ' ── Settings keys ───────────────────────────────────────────────────────
    Public Const ChangeLoading As String = "ChangeLoading"
    Public Const Additional As String = "Additional"
    Public Const Approver As String = "Approver"

    Private Sub New()
    End Sub
End Class
