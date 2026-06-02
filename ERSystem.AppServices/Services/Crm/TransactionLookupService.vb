Option Strict On

Imports ERSystem.Domain
Imports ERSystem.Infrastructure.Configuration
Imports ERSystem.Infrastructure.Data

Namespace Global.ERSystem.AppServices.Services.Crm
    Public Class TransactionLookupService
        Private ReadOnly _repository As ITransactionRepository
        Private ReadOnly _userAccountRegistryProvider As UserAccountRegistryProvider

        Public Sub New()
            Me.New(New TransactionRepository(), New UserAccountRegistryProvider())
        End Sub

        Public Sub New(repository As ITransactionRepository)
            Me.New(repository, New UserAccountRegistryProvider())
        End Sub

        Public Sub New(repository As ITransactionRepository, userAccountRegistryProvider As UserAccountRegistryProvider)
            If repository Is Nothing Then
                Throw New ArgumentNullException("repository")
            End If

            If userAccountRegistryProvider Is Nothing Then
                Throw New ArgumentNullException("userAccountRegistryProvider")
            End If

            _repository = repository
            _userAccountRegistryProvider = userAccountRegistryProvider
        End Sub

        Public Function GetFWMSBySRNumber(srNumber As String) As FwmsTransactionDto
            If String.IsNullOrWhiteSpace(srNumber) Then
                Return Nothing
            End If

            Return _repository.GetFWMSBySRNumber(srNumber.Trim())
        End Function

        Public Function GetFWMSByTransactionDateRange(dateFrom As Date, dateTo As Date) As List(Of FwmsTransactionDto)
            If dateTo.Date < dateFrom.Date Then
                Return New List(Of FwmsTransactionDto)()
            End If

            Dim userInitial As String = _userAccountRegistryProvider.GetValue("username")

            Return _repository.GetFWMSByTransactionDateRange(dateFrom.Date, dateTo.Date, userInitial)
        End Function
    End Class
End Namespace
