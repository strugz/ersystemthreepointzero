Option Strict On

Imports ERSystem.Domain
Imports System.Collections.Generic
Imports System.Net
Imports System.Text

Public Class SmsNotificationService
    Implements ISmsNotificationService

    Private Const SmsApiUrl As String = "https://mdmpi.com.ph/lasius/api_sendsms"

    Public Function Send(notification As SmsNotificationRequestDto) As SmsNotificationResult Implements ISmsNotificationService.Send
        If notification Is Nothing Then
            Return Failed("SMS request is required.")
        End If

        Dim recipients As List(Of SmsRecipientDto) = ResolveRecipients(notification)
        If recipients.Count = 0 Then
            Return Failed("No SMS recipients were found.")
        End If

        Dim sender As String = If(notification.Sender, notification.UserInitial)
        If String.IsNullOrWhiteSpace(sender) Then
            Return Failed("SMS sender is required before sending.")
        End If

        If String.IsNullOrWhiteSpace(notification.Message) Then
            Return Failed("Message is required before sending SMS.")
        End If

        Dim sentCount As Integer = 0
        Dim failedRecipients As New List(Of String)()

        For Each recipient As SmsRecipientDto In recipients
            Dim sendResult As SmsNotificationResult = SendSingle(recipient.Username, sender, notification.Message)
            If sendResult.IsSuccess Then
                sentCount += 1
            Else
                failedRecipients.Add(recipient.Username)
            End If
        Next

        Dim summary As String = "SMS sending completed. Sent: " & sentCount.ToString() & ", Failed: " & failedRecipients.Count.ToString()
        If failedRecipients.Count > 0 Then
            summary &= ". Failed recipients: " & String.Join(", ", failedRecipients.ToArray())
        End If

        Return New SmsNotificationResult With {
            .IsSuccess = sentCount > 0 AndAlso failedRecipients.Count = 0,
            .Message = summary
        }
    End Function

    Private Shared Function ResolveRecipients(notification As SmsNotificationRequestDto) As List(Of SmsRecipientDto)
        Dim recipients As New List(Of SmsRecipientDto)()
        Dim seenUsernames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        If notification.Recipients IsNot Nothing Then
            For Each recipient As SmsRecipientDto In notification.Recipients
                If recipient IsNot Nothing Then
                    AddRecipient(recipients, seenUsernames, recipient.Username, recipient.Fullname, recipient.RecipientType)
                End If
            Next
        End If

        AddRecipient(recipients, seenUsernames, notification.Receiver, String.Empty, "Receiver")
        Return recipients
    End Function

    Private Shared Sub AddRecipient(recipients As List(Of SmsRecipientDto),
                                    seenUsernames As HashSet(Of String),
                                    username As String,
                                    fullname As String,
                                    recipientType As String)
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
            .RecipientType = If(recipientType, String.Empty)
        })
    End Sub

    Private Shared Function SendSingle(receiver As String, sender As String, message As String) As SmsNotificationResult
        Try
            Dim payload As String = "{" &
                """RECEIVER"":""" & EscapeJson(receiver.Trim()) & """," &
                """SENDER"":""" & EscapeJson(sender.Trim()) & """," &
                """MESSAGE"":""" & EscapeJson(message.Trim()) & """" &
                "}"

            Dim payloadBytes As Byte() = Encoding.UTF8.GetBytes(payload)
            Dim httpRequest As HttpWebRequest = CType(System.Net.WebRequest.Create(SmsApiUrl), HttpWebRequest)
            httpRequest.Method = "POST"
            httpRequest.ContentType = "application/json"
            httpRequest.Accept = "*/*"
            httpRequest.Headers(HttpRequestHeader.AcceptEncoding) = "gzip, deflate, br"
            httpRequest.KeepAlive = True
            httpRequest.ContentLength = payloadBytes.Length

            Using requestStream = httpRequest.GetRequestStream()
                requestStream.Write(payloadBytes, 0, payloadBytes.Length)
            End Using

            Using response As HttpWebResponse = CType(httpRequest.GetResponse(), HttpWebResponse)
                If response.StatusCode = HttpStatusCode.OK Then
                    Return New SmsNotificationResult With {
                        .IsSuccess = True,
                        .Message = "SMS sent successfully."
                    }
                End If

                Return Failed("SMS sending failed. API status: " & CInt(response.StatusCode).ToString())
            End Using
        Catch ex As WebException
            Return Failed("SMS sending failed. " & ex.Message)
        Catch ex As Exception
            Return Failed("SMS sending failed. " & ex.Message)
        End Try
    End Function

    Private Shared Function Failed(message As String) As SmsNotificationResult
        Return New SmsNotificationResult With {
            .IsSuccess = False,
            .Message = message
        }
    End Function

    Private Shared Function EscapeJson(value As String) As String
        If value Is Nothing Then
            Return String.Empty
        End If

        Return value.Replace("\", "\\").
            Replace("""", "\""").
            Replace(vbCr, "\r").
            Replace(vbLf, "\n").
            Replace(vbTab, "\t")
    End Function
End Class
