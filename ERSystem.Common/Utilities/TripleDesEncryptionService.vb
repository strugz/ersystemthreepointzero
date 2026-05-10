Imports System.Security.Cryptography
Imports ERSystem.Common.Interfaces

Namespace Utilities
    Public Class TripleDesEncryptionService
        Implements IEncryptionService

        Private ReadOnly _tripleDes As New TripleDESCryptoServiceProvider

        Private Function TruncateHash(key As String, length As Integer) As Byte()
            Dim sha1 As New SHA1CryptoServiceProvider
            Dim keyBytes() As Byte = System.Text.Encoding.Unicode.GetBytes(key)
            Dim hash() As Byte = sha1.ComputeHash(keyBytes)
            ReDim Preserve hash(length - 1)
            Return hash
        End Function

        Public Sub New(key As String)
            _tripleDes.Key = TruncateHash(key, _tripleDes.KeySize \ 8)
            _tripleDes.IV = TruncateHash("", _tripleDes.BlockSize \ 8)
        End Sub

        Public Function EncryptData(plaintext As String) As String Implements IEncryptionService.EncryptData
            If String.IsNullOrEmpty(plaintext) Then Return String.Empty

            Dim plaintextBytes() As Byte = System.Text.Encoding.Unicode.GetBytes(plaintext)
            Dim ms As New System.IO.MemoryStream
            Dim encStream As New CryptoStream(ms, _tripleDes.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write)
            encStream.Write(plaintextBytes, 0, plaintextBytes.Length)
            encStream.FlushFinalBlock()
            Return EnHex(Convert.ToBase64String(ms.ToArray))
        End Function

        Public Function DecryptData(encryptedtext As String) As String Implements IEncryptionService.DecryptData
             If String.IsNullOrEmpty(encryptedtext) Then Return String.Empty

            Dim encryptedBytes() As Byte = Convert.FromBase64String(DeHex(encryptedtext))
            Dim ms As New System.IO.MemoryStream
            Dim decStream As New CryptoStream(ms, _tripleDes.CreateDecryptor(), System.Security.Cryptography.CryptoStreamMode.Write)
            If encryptedBytes.Length > 0 Then
                decStream.Write(encryptedBytes, 0, encryptedBytes.Length)
                decStream.FlushFinalBlock()
            End If
            Return System.Text.Encoding.Unicode.GetString(ms.ToArray)
        End Function

        Private Function EnHex(Data As String) As String
            Dim iCount As Double, sTemp As String, tempStr As String
            tempStr = ""
            For iCount = 1 To Len(Data)
                sTemp = Hex$(Asc(Mid$(Data, iCount, 1)))
                If Len(sTemp) < 2 Then sTemp = "0" & sTemp
                tempStr = tempStr & sTemp
            Next
            Return tempStr
        End Function

        Private Function DeHex(Data As String) As String
            Dim iCount As Double, tempStr As String
            tempStr = ""
            For iCount = 1 To Len(Data) Step 2
                tempStr = tempStr + Chr(Val("&H" & Mid(Data, iCount, 2)))
            Next
            Return tempStr
        End Function
    End Class
End Namespace