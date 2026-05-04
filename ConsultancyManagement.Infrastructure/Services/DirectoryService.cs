using System.Security.Claims;
using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ConsultancyManagement.Infrastructure.Data;

namespace ConsultancyManagement.Infrastructure.Services;

public class DirectoryService : IDirectoryService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DirectoryService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<DirectoryUserEntryDto>> GetVisibleUsersAsync(ClaimsPrincipal principal)
    {
        var userId = UserContextHelper.GetUserId(principal);
        if (string.IsNullOrEmpty(userId)) return Array.Empty<DirectoryUserEntryDto>();

        var me = await _userManager.FindByIdAsync(userId);
        if (me is null) return Array.Empty<DirectoryUserEntryDto>();

        if (principal.IsInRole(nameof(UserRole.Admin)))
            return await MapUsersAsync(await _userManager.Users.AsNoTracking()
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.EmployeeId)
                .Select(u => u.Id)
                .ToListAsync());

        if (principal.IsInRole(nameof(UserRole.Management)))
        {
            var ids = new HashSet<string>();
            foreach (var id in await _db.Consultants.AsNoTracking().Select(c => c.UserId).ToListAsync())
                ids.Add(id);
            foreach (var id in await _db.SalesRecruiters.AsNoTracking().Select(s => s.UserId).ToListAsync())
                ids.Add(id);
            foreach (var id in await _db.ManagementUsers.AsNoTracking().Select(m => m.UserId).ToListAsync())
                ids.Add(id);
            return await MapUsersAsync(ids.OrderBy(x => x).ToList());
        }

        if (principal.IsInRole(nameof(UserRole.SalesRecruiter)))
        {
            var ids = new HashSet<string> { userId };
            var sales = await _db.SalesRecruiters.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId);
            if (sales is not null)
            {
                var consultantUserIds = await _db.ConsultantSalesAssignments.AsNoTracking()
                    .Where(a => a.SalesRecruiterId == sales.Id && a.IsActive)
                    .Select(a => a.Consultant.UserId)
                    .ToListAsync();
                foreach (var cid in consultantUserIds)
                    ids.Add(cid);
            }

            return await MapUsersAsync(ids.ToList());
        }

        if (principal.IsInRole(nameof(UserRole.Consultant)))
            return await MapUsersAsync(new List<string> { userId });

        return Array.Empty<DirectoryUserEntryDto>();
    }

    private async Task<IReadOnlyList<DirectoryUserEntryDto>> MapUsersAsync(IReadOnlyList<string> userIds)
    {
        if (userIds.Count == 0) return Array.Empty<DirectoryUserEntryDto>();

        var list = new List<DirectoryUserEntryDto>();
        foreach (var id in userIds.Distinct())
        {
            var u = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u is null || u.IsDeleted || string.IsNullOrEmpty(u.EmployeeId)) continue;
            var loaded = await _userManager.FindByIdAsync(u.Id);
            if (loaded is null) continue;
            var roles = await _userManager.GetRolesAsync(loaded);
            list.Add(new DirectoryUserEntryDto
            {
                EmployeeId = u.EmployeeId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                Roles = roles.ToList()
            });
        }

        return list.OrderBy(x => x.EmployeeId).ToList();
    }
}
