Option Strict On

Imports System.Security.Cryptography
Imports System.Text

Public Class AccountSettingsValueProtector
    Implements IAccountSettingsValueProtector

    Private Const EncryptionKey As String = "crimsonmonastery2003"
    Private ReadOnly _encryption As New LegacyEncryption(EncryptionKey)

    Public Function Protect(value As String) As String Implements IAccountSettingsValueProtector.Protect
        Return _encryption.EncryptData(value)
    End Function

    Public Function Unprotect(value As String) As String Implements IAccountSettingsValueProtector.Unprotect
        If String.IsNullOrWhiteSpace(value) Then
            Return String.Empty
        End If

        Try
            Return _encryption.DecryptData(value)
        Catch ex As Exception
            Debug.WriteLine("Unable to decrypt account settings value: " & ex.Message)
            Return value
        End Try
    End Function

    Private NotInheritable Class LegacyEncryption
        Private ReadOnly _tripleDes As New TripleDESCryptoServiceProvider()

        Public Sub New(key As String)
            _tripleDes.Key = TruncateHash(key, _tripleDes.KeySize \ 8)
            _tripleDes.IV = TruncateHash(String.Empty, _tripleDes.BlockSize \ 8)
        End Sub

        Public Function EncryptData(plainText As String) As String
            Dim plainTextBytes As Byte() = Encoding.Unicode.GetBytes(plainText)

            Using memoryStream As New IO.MemoryStream()
                Using encryptStream As New CryptoStream(memoryStream, _tripleDes.CreateEncryptor(), CryptoStreamMode.Write)
                    encryptStream.Write(plainTextBytes, 0, plainTextBytes.Length)
                    encryptStream.FlushFinalBlock()
                End Using

                Return ToHex(Convert.ToBase64String(memoryStream.ToArray()))
            End Using
        End Function

        Public Function DecryptData(encryptedText As String) As String
            Dim encryptedBytes As Byte() = Convert.FromBase64String(FromHex(encryptedText))

            Using memoryStream As New IO.MemoryStream()
                Using decryptStream As New CryptoStream(memoryStream, _tripleDes.CreateDecryptor(), CryptoStreamMode.Write)
                    If encryptedBytes.Length > 0 Then
                        decryptStream.Write(encryptedBytes, 0, encryptedBytes.Length)
                        decryptStream.FlushFinalBlock()
                    End If
                End Using

                Return Encoding.Unicode.GetString(memoryStream.ToArray())
            End Using
        End Function

        Private Shared Function TruncateHash(key As String, length As Integer) As Byte()
            Using sha1 As New SHA1CryptoServiceProvider()
                Dim keyBytes As Byte() = Encoding.Unicode.GetBytes(key)
                Dim hash As Byte() = sha1.ComputeHash(keyBytes)
                ReDim Preserve hash(length - 1)
                Return hash
            End Using
        End Function

        Private Shared Function ToHex(data As String) As String
            Dim builder As New StringBuilder()

            For index As Integer = 0 To data.Length - 1
                builder.Append(AscW(data(index)).ToString("X2"))
            Next

            Return builder.ToString()
        End Function

        Private Shared Function FromHex(data As String) As String
            Dim builder As New StringBuilder()

            For index As Integer = 0 To data.Length - 1 Step 2
                builder.Append(ChrW(Convert.ToInt32(data.Substring(index, 2), 16)))
            Next

            Return builder.ToString()
        End Function
    End Class
End Class
