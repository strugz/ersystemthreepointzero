Imports System.Data

Namespace Domain.Interfaces
    Public Interface IDepartmentRepository
        Function LoadingDepartment() As DataTable
    End Interface
End Namespace