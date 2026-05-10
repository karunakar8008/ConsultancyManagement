using System.Security.Claims;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ConsultancyManagement.Infrastructure.Services;

public class CurrentOrganization : ICurrentOrganization
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentOrganization(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public int OrganizationId
    {
        get
        {
            var v = _httpContextAccessor.HttpContext?.User.FindFirstValue("organization_id");
            return int.TryParse(v, out var id) ? id : 0;
        }
    }
}
