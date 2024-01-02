Imports System.IO
Module modReuse
    Private Declare Function SetProcessWorkingSetSize Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal dwMinimumWorkingSetSize As Int32, ByVal dwMaximumWorkingSetSize As Int32) As Int32
    Public Function SetTextFile(ByVal workwith As String, ByVal hospital As String,
                              ByVal instrument As String, ByVal serialnumber As String,
                                   ByVal servicenumber As String) As String
        Dim str2 As String = ""
        Dim str() As String = {workwith, hospital, instrument, serialnumber, servicenumber}
        Dim objWriter As New StreamWriter(Application.StartupPath + "/ER.txt")
        If File.Exists(Application.StartupPath + "/ER.txt") = False Then
            Directory.CreateDirectory(Application.StartupPath + "/ER.txt")
            For i = 0 To str.Count - 1
                If i = str.Count - 1 Then
                    objWriter.Write(str(i))
                Else
                    objWriter.Write(str(i) + "/")
                End If
            Next
        Else
            For i = 0 To str.Count - 1
                If i = str.Count - 1 Then
                    objWriter.Write(str(i))
                Else
                    objWriter.Write(str(i) + "/")
                End If
            Next
            objWriter.Close()
        End If
        Return str2
    End Function
    Public Function GetTextFile() As String
        GetTextFile = My.Computer.FileSystem.ReadAllText(Application.StartupPath + "/ER.txt")
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
