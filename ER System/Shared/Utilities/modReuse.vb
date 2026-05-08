Imports System.IO
Module modReuse
    Private ReadOnly StartupPath As String = System.Windows.Forms.Application.StartupPath
    Private Declare Function SetProcessWorkingSetSize Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal dwMinimumWorkingSetSize As Int32, ByVal dwMaximumWorkingSetSize As Int32) As Int32
    Public Function SetTextFile(ByVal workwith As String, ByVal hospital As String,
                              ByVal instrument As String, ByVal serialnumber As String,
                                   ByVal servicenumber As String) As String
        Dim str As String = ""
        Dim str2 As String = ""
        Dim strItems() As String = {workwith, hospital, instrument, serialnumber, servicenumber}

        If Not File.Exists(StartupPath + "/ER.txt") Then
            Directory.CreateDirectory(StartupPath)
        End If

        Using objWriter As New StreamWriter(StartupPath + "/ER.txt")
            For i = 0 To strItems.Length - 1
                If i = strItems.Length - 1 Then
                    objWriter.Write(strItems(i))
                Else
                    objWriter.Write(strItems(i) + "/")
                End If
            Next
        End Using

        Return str2
    End Function
    Public Function GetTextFile() As String
        GetTextFile = My.Computer.FileSystem.ReadAllText(StartupPath + "/ER.txt")
    End Function
    Public Sub ReleasMemory()
        Try
            GC.Collect()
            GC.WaitForPendingFinalizers()
            If Environment.OSVersion.Platform = PlatformID.Win32NT Then
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1)
            End If
        Catch ex As Exception
            MsgBox(ex.ToString())
        End Try
    End Sub
End Module
