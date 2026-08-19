Imports System.Collections.Generic
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class UserRegistrationService
        Implements IUserRegistrationService

        Private ReadOnly _repository As IUserRegistrationRepository

        Public Sub New()
            Me.New(New UserRegistrationRepository())
        End Sub

        Public Sub New(repository As IUserRegistrationRepository)
            If repository Is Nothing Then
                Throw New ArgumentNullException("repository")
            End If

            _repository = repository
        End Sub

        Public Function ResolveSmsRecipientsForUser(userId As Nullable(Of Integer)) As List(Of SmsRecipientDto) Implements IUserRegistrationService.ResolveSmsRecipientsForUser
            Dim recipients As New List(Of SmsRecipientDto)()
            Dim seenUsernames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            If Not userId.HasValue Then
                Return recipients
            End If

            Dim erfUser As UserRegistrationModel = _repository.GetByUserId(userId.Value)
            If erfUser Is Nothing Then
                Return recipients
            End If

            AddRecipient(recipients, seenUsernames, "ERF User", erfUser.Username, erfUser.Fullname)
            AddApproverRecipient(recipients, seenUsernames, "Approver 1", erfUser.Approver1)
            AddApproverRecipient(recipients, seenUsernames, "Approver 2", erfUser.Approver2)

            Return recipients
        End Function

        Private Sub AddApproverRecipient(recipients As List(Of SmsRecipientDto),
                                         seenUsernames As HashSet(Of String),
                                         recipientType As String,
                                         username As String)
            If String.IsNullOrWhiteSpace(username) Then
                Return
            End If

            Dim approver As UserRegistrationModel = _repository.GetByUsername(username.Trim())
            AddRecipient(
                recipients,
                seenUsernames,
                recipientType,
                username,
                If(approver Is Nothing, String.Empty, approver.Fullname))
        End Sub

        Private Shared Sub AddRecipient(recipients As List(Of SmsRecipientDto),
                                        seenUsernames As HashSet(Of String),
                                        recipientType As String,
                                        username As String,
                                        fullname As String)
            If String.IsNullOrWhiteSpace(username) Then
                Return
            End If

            Dim normalizedUsername As String = username.Trim()
            If seenUsernames.Contains(normalizedUsername) Then
                Return
            End If

            seenUsernames.Add(normalizedUsername)
            recipients.Add(New SmsRecipientDto With {
                .Username = normalizedUsername,
                .Fullname = If(fullname, String.Empty),
                .RecipientType = recipientType
            })
        End Sub
    End Class
End Namespace
