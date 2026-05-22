Imports ERSystem.Domain

Namespace AppServices
    Friend Interface ISmsNotificationService
        Function Send(request As SmsNotificationRequestDto) As SmsNotificationResult
    End Interface
End Namespace
