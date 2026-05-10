Namespace Interfaces
    Public Interface IEncryptionService
        Function EncryptData(plaintext As String) As String
        Function DecryptData(encryptedtext As String) As String
    End Interface
End Namespace