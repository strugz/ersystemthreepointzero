Imports ERSystem.Infrastructure

Namespace Global.ERSystem.Infrastructure.Data
    Friend NotInheritable Class AppDbContextProvider
        Private Sub New()
        End Sub

        Private Shared _current As AppDbContext

        Friend Shared ReadOnly Property Current As AppDbContext
            Get
                If _current Is Nothing Then
                    _current = New AppDbContext()
                End If

                Return _current
            End Get
        End Property
    End Class
End Namespace
