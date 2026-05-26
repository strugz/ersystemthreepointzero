Option Strict On

Imports System.Linq
Imports ERSystem.Domain

Namespace Global.ERSystem.Infrastructure.Data
    Public NotInheritable Class ExpenseNotificationRepository
        Implements IExpenseNotificationRepository

        Public Function Create(notification As ExpenseNotificationDto, dbContext As AppDbContext) As ExpenseNotificationDto Implements IExpenseNotificationRepository.Create
            If notification Is Nothing Then
                Throw New ArgumentNullException("notification")
            End If

            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim model As ExpenseNotificationModel = ToModel(notification)
            dbContext.ExpenseNotifications.Add(model)
            dbContext.SaveChanges()
            Return ToDto(model)
        End Function

        Public Sub DeleteByExpenseId(expenseId As Long, dbContext As AppDbContext) Implements IExpenseNotificationRepository.DeleteByExpenseId
            If dbContext Is Nothing Then
                Throw New ArgumentNullException("dbContext")
            End If

            Dim expenseIdValue As String = expenseId.ToString()
            Dim notifications = dbContext.ExpenseNotifications.Where(Function(item) item.ExpenseID = expenseIdValue).ToList()

            If notifications.Count = 0 Then
                Return
            End If

            dbContext.ExpenseNotifications.RemoveRange(notifications)
            dbContext.SaveChanges()
        End Sub

        Private Shared Function ToModel(notification As ExpenseNotificationDto) As ExpenseNotificationModel
            Return New ExpenseNotificationModel With {
                .ID = notification.ID,
                .ToNotify = notification.ToNotify,
                .DateIncluded = notification.DateIncluded,
                .ExpenseID = notification.ExpenseID,
                .Category = notification.Category,
                .UsernameFiled = notification.UsernameFiled,
                .Status = notification.Status
            }
        End Function

        Private Shared Function ToDto(notification As ExpenseNotificationModel) As ExpenseNotificationDto
            Return New ExpenseNotificationDto With {
                .ID = notification.ID,
                .ToNotify = notification.ToNotify,
                .DateIncluded = notification.DateIncluded,
                .ExpenseID = notification.ExpenseID,
                .Category = notification.Category,
                .UsernameFiled = notification.UsernameFiled,
                .Status = notification.Status
            }
        End Function
    End Class
End Namespace
