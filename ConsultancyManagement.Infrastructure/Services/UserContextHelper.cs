using System.Security.Claims;

namespace ConsultancyManagement.Infrastructure.Services;

public static class UserContextHelper
{
    public static string? GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static bool IsInAnyRole(ClaimsPrincipal user, params string[] roles) =>
        roles.Any(r => user.IsInRole(r));
}
