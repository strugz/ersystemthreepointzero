Option Strict On

Imports ERSystem.Domain

Public Interface ISmsNotificationService
    Function Send(request As SmsNotificationRequestDto) As SmsNotificationResult
End Interface
