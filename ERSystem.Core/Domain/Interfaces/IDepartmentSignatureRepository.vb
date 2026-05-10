Imports ERSystem.Core.Domain.Entities

Namespace Domain.Interfaces
    Public Interface IDepartmentSignatureRepository
        Sub AddDepartmentSignature(deptSign As DepartmentSignature)
        Sub UpdateDepartmentSignature(deptSign As DepartmentSignature)
    End Interface
End Namespace