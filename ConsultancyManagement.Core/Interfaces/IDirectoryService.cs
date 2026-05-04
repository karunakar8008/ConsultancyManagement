using System.Security.Claims;
using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface IDirectoryService
{
    Task<IReadOnlyList<DirectoryUserEntryDto>> GetVisibleUsersAsync(ClaimsPrincipal principal);
}
