Imports ERSystem.Core.Domain.Entities
Imports ERSystem.Core.Domain.Interfaces
Imports ERSystem.Core.Application.Interfaces

Namespace Application.Services
    Public Class DepartmentSignatureService
        Implements IDepartmentSignatureService

        Private ReadOnly _repository As IDepartmentSignatureRepository

        Public Sub New(repository As IDepartmentSignatureRepository)
            _repository = repository
        End Sub

        Public Sub AddDepartmentSignature(deptSign As DepartmentSignature) Implements IDepartmentSignatureService.AddDepartmentSignature
            _repository.AddDepartmentSignature(deptSign)
        End Sub

        Public Sub UpdateDepartmentSignature(deptSign As DepartmentSignature) Implements IDepartmentSignatureService.UpdateDepartmentSignature
            _repository.UpdateDepartmentSignature(deptSign)
        End Sub
    End Class
End Namespace
