using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface IPlatformService
{
    Task<IReadOnlyList<OrganizationListItemDto>> ListOrganizationsAsync();
    Task<(bool Success, string? Error, int? OrganizationId)> CreateOrganizationAsync(CreateOrganizationRequestDto dto);
    Task<(bool Success, string? Error)> BootstrapOrganizationAdminAsync(int organizationId, BootstrapOrgAdminRequestDto dto);
}
