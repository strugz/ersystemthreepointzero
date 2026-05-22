Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public Interface IUserRegistrationService
        Function ResolveSmsRecipientsForUser(userId As Nullable(Of Integer)) As List(Of SmsRecipientDto)
    End Interface
End Namespace
