using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Data;
using ConsultancyManagement.Infrastructure.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConsultancyManagement.Infrastructure.Services;

public class PlatformService : IPlatformService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PlatformService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<OrganizationListItemDto>> ListOrganizationsAsync() =>
        await _db.Organizations.AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new OrganizationListItemDto
            {
                Id = o.Id,
                Name = o.Name,
                Slug = o.Slug,
                IsActive = o.IsActive
            })
            .ToListAsync();

    public async Task<(bool Success, string? Error, int? OrganizationId)> CreateOrganizationAsync(CreateOrganizationRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) return (false, "Organization name is required.", null);
        var slug = OrganizationSlugHelper.Normalize(dto.Slug);
        if (!OrganizationSlugHelper.IsValidSlug(slug)) return (false, "Slug must be 2–64 characters: lowercase letters, digits, hyphens; no leading/trailing hyphen.", null);
        if (await _db.Organizations.AnyAsync(o => o.Slug == slug))
            return (false, "That organization slug is already in use.", null);

        var org = new Organization
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();
        return (true, null, org.Id);
    }

    public async Task<(bool Success, string? Error)> BootstrapOrganizationAdminAsync(int organizationId, BootstrapOrgAdminRequestDto dto)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId);
        if (org is null) return (false, "Organization not found.");

        var adminRole = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == UserRole.Admin.ToString());
        if (adminRole is null) return (false, "Admin role is not configured.");

        var hasAdmin = await (from u in _db.Users
            join ur in _db.UserRoles on u.Id equals ur.UserId
            where u.OrganizationId == organizationId && !u.IsDeleted && ur.RoleId == adminRole.Id
            select u).AnyAsync();
        if (hasAdmin) return (false, "This organization already has an admin user.");

        if (!ValidationHelper.IsValidEmail(dto.Email)) return (false, "A valid email is required.");
        if (string.IsNullOrWhiteSpace(dto.Password)) return (false, "Password is required.");

        var norm = _userManager.NormalizeEmail(dto.Email.Trim());
        if (await _userManager.Users.AnyAsync(u => u.OrganizationId == organizationId && !u.IsDeleted && u.NormalizedEmail == norm))
            return (false, "A user with this email already exists in this organization.");

        var prefix = EmployeeIdGenerator.GetPrefix(UserRole.Admin);
        var maxSuffix = await _userManager.Users
            .Where(u => u.OrganizationId == organizationId && !string.IsNullOrEmpty(u.EmployeeId) && u.EmployeeId.StartsWith(prefix))
            .Select(u => u.EmployeeId)
            .ToListAsync();
        var next = 1;
        if (maxSuffix.Count > 0)
        {
            var n = maxSuffix
                .Select(id => id.Length > 3 && int.TryParse(id[3..], out var x) ? x : 0)
                .DefaultIfEmpty(0)
                .Max();
            next = n + 1;
        }

        var user = new ApplicationUser
        {
            OrganizationId = organizationId,
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            EmailConfirmed = true,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmployeeId = EmployeeIdGenerator.Build(prefix, next)
        };

        var create = await _userManager.CreateAsync(user, dto.Password);
        if (!create.Succeeded)
            return (false, string.Join(" ", create.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, UserRole.Admin.ToString());
        return (true, null);
    }
}
