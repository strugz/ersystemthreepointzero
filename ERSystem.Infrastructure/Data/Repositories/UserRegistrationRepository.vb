Imports System.Data.Entity
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class UserRegistrationRepository
        Implements IUserRegistrationRepository

        Public Function GetByUserId(userId As Integer) As UserRegistrationModel Implements IUserRegistrationRepository.GetByUserId
            Using dbContext As New AppDbContext()
                Return dbContext.UserRegistrations.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.UserID.HasValue AndAlso item.UserID.Value = userId)
            End Using
        End Function

        Public Function GetByUsername(username As String) As UserRegistrationModel Implements IUserRegistrationRepository.GetByUsername
            If String.IsNullOrWhiteSpace(username) Then
                Return Nothing
            End If

            Dim normalizedUsername As String = username.Trim()
            Using dbContext As New AppDbContext()
                Return dbContext.UserRegistrations.
                    AsNoTracking().
                    FirstOrDefault(Function(item) item.Username = normalizedUsername)
            End Using
        End Function
    End Class
End Namespace
