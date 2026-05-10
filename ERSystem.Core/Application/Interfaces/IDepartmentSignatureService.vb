Imports ERSystem.Core.Domain.Entities

Namespace Application.Interfaces
    Public Interface IDepartmentSignatureService
        Sub AddDepartmentSignature(deptSign As DepartmentSignature)
        Sub UpdateDepartmentSignature(deptSign As DepartmentSignature)
    End Interface
End Namespace