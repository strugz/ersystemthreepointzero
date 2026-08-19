Option Strict On

Imports ERSystem.AppServices
Imports ERSystem.Domain
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Namespace Services
    <TestClass>
    Public Class ScannedReceiptSelectionStateTests
        <TestMethod>
        Public Sub BlankLegacyPathWithStoredReceiptRemainsUnchanged()
            Dim state As New ScannedReceiptSelectionState(
                String.Empty,
                {New ScannedReceiptAttachmentMetadataDto With {.ID = 21, .OriginalFileName = "receipt.pdf"}},
                False)

            Assert.AreEqual(ScannedReceiptAttachmentUpdateMode.Unchanged, state.UpdateMode)
            Assert.IsTrue(state.HasReceipts)
            Assert.AreEqual("receipt.pdf", state.BuildDisplayText())
        End Sub

        <TestMethod>
        Public Sub ExplicitClearRequestsReplacementWithNoPaths()
            Dim state As New ScannedReceiptSelectionState(
                "C:\Receipts\receipt.pdf",
                {New ScannedReceiptAttachmentMetadataDto With {.OriginalFileName = "receipt.pdf"}},
                False)

            state.Clear()

            Assert.AreEqual(ScannedReceiptAttachmentUpdateMode.Replace, state.UpdateMode)
            Assert.IsFalse(state.HasReceipts)
            Assert.AreEqual(String.Empty, state.BuildLegacyAttachmentValue(Enumerable.Empty(Of String)()))
        End Sub

        <TestMethod>
        Public Sub BrowseAppendsOnlyUniqueNewPaths()
            Dim state As New ScannedReceiptSelectionState("C:\Receipts\existing.pdf", Nothing, False)

            Dim added As Integer = state.AddLocalPaths({"C:\Receipts\existing.pdf", "C:\Receipts\new.png", "C:\Receipts\new.png"})

            Assert.AreEqual(1, added)
            Assert.AreEqual(ScannedReceiptAttachmentUpdateMode.Append, state.UpdateMode)
            CollectionAssert.AreEqual(New List(Of String) From {"C:\Receipts\new.png"}, state.GetPendingLocalPaths())
        End Sub

        <TestMethod>
        Public Sub ApprovedReceiptSelectionIsReadOnly()
            Dim state As New ScannedReceiptSelectionState(String.Empty, Nothing, True)

            Dim clearFailed As Boolean
            Try
                state.Clear()
            Catch ex As InvalidOperationException
                clearFailed = True
            End Try

            Dim browseFailed As Boolean
            Try
                state.AddLocalPaths({"C:\Receipts\new.pdf"})
            Catch ex As InvalidOperationException
                browseFailed = True
            End Try

            Assert.IsTrue(clearFailed)
            Assert.IsTrue(browseFailed)
        End Sub
    End Class
End Namespace
