using ERP.Domain.Entities.Modules.HumanResourcesManagement;

namespace ERP.Application.UseCases.Modules.HumanResourcesManagement.DTOs
{
    public record AddEditHumanDto
    {
        public string Username { get; init; } = default!;
        public string Password { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string Family { get; init; } = default!;
        public HumanGender Gender { get; init; }
        public DateOnly BirthDate { get; init; }
        public bool MaritalStatus { get; init; }
        public string NationalId { get; init; } = default!;
        public string? PassportId { get; init; }
    }
}
