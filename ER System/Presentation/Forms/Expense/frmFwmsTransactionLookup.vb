Imports ERSystem.AppServices.Services.Crm
Imports ERSystem.Domain

Public Class frmFwmsTransactionLookup
    Private ReadOnly _transactionLookupService As New TransactionLookupService()

    Public Property SelectedTransaction As FwmsTransactionDto

    Private Sub frmFwmsTransactionLookup_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        dtpDateFrom.Value = Date.Today
        dtpDateTo.Value = Date.Today
        ConfigureGrid()
        LoadTransactions()
    End Sub

    Private Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click
        LoadTransactions()
    End Sub

    Private Sub btnInsert_Click(sender As Object, e As EventArgs) Handles btnInsert.Click
        InsertSelectedTransaction()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        DialogResult = DialogResult.Cancel
        Close()
    End Sub

    Private Sub dgvTransactions_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvTransactions.CellDoubleClick
        If e.RowIndex >= 0 Then
            InsertSelectedTransaction()
        End If
    End Sub

    Private Sub LoadTransactions()
        Try
            Dim transactions As List(Of FwmsTransactionDto) =
                _transactionLookupService.GetFWMSByTransactionDateRange(dtpDateFrom.Value.Date, dtpDateTo.Value.Date)

            dgvTransactions.AutoGenerateColumns = False
            dgvTransactions.DataSource = transactions
        Catch ex As Exception
            MessageBox.Show("Unable to load FWMS transactions. " & BuildExceptionMessage(ex), "FWMS Lookup", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ConfigureGrid()
        dgvTransactions.AutoGenerateColumns = False
        dgvTransactions.Columns.Clear()
        dgvTransactions.Columns.Add(CreateTextColumn("HospitalName", "Hospital Name"))
        dgvTransactions.Columns.Add(CreateTextColumn("InstrumentModel", "Instrument Model"))
        dgvTransactions.Columns.Add(CreateTextColumn("SRNumber", "S.R. No"))
        dgvTransactions.Columns.Add(CreateTextColumn("SerialNumber", "Serial Number"))
    End Sub

    Private Shared Function CreateTextColumn(dataPropertyName As String, headerText As String) As DataGridViewTextBoxColumn
        Return New DataGridViewTextBoxColumn With {
            .DataPropertyName = dataPropertyName,
            .HeaderText = headerText,
            .Name = dataPropertyName,
            .ReadOnly = True,
            .SortMode = DataGridViewColumnSortMode.Automatic
        }
    End Function

    Private Sub InsertSelectedTransaction()
        If dgvTransactions.CurrentRow Is Nothing Then
            Return
        End If

        Dim transaction As FwmsTransactionDto = TryCast(dgvTransactions.CurrentRow.DataBoundItem, FwmsTransactionDto)
        If transaction Is Nothing Then
            Return
        End If

        SelectedTransaction = transaction
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Shared Function BuildExceptionMessage(ex As Exception) As String
        Dim messages As New List(Of String)()
        Dim current As Exception = ex

        While current IsNot Nothing
            If Not String.IsNullOrWhiteSpace(current.Message) Then
                messages.Add(current.Message)
            End If

            current = current.InnerException
        End While

        Return String.Join(" ", messages.ToArray())
    End Function
End Class
