Option Strict On

Imports System.Security.Cryptography
Imports System.Text

Namespace Global.ERSystem.Infrastructure.Configuration
    Friend NotInheritable Class LegacyValueProtector
        Private ReadOnly _tripleDes As New TripleDESCryptoServiceProvider()

        Public Sub New(key As String)
            _tripleDes.Key = TruncateHash(key, _tripleDes.KeySize \ 8)
            _tripleDes.IV = TruncateHash(String.Empty, _tripleDes.BlockSize \ 8)
        End Sub

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

        Private Shared Function FromHex(data As String) As String
            Dim builder As New StringBuilder()

            For index As Integer = 0 To data.Length - 1 Step 2
                builder.Append(ChrW(Convert.ToInt32(data.Substring(index, 2), 16)))
            Next

            Return builder.ToString()
        End Function
    End Class
End Namespace
