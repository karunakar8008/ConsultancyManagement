using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ConsultancyManagement.Infrastructure.Services;

namespace ConsultancyManagement.Infrastructure.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<Data.ApplicationDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Database.MigrateAsync failed (install dotnet-ef and run migrations). Using EnsureCreated for development.");
            await context.Database.EnsureCreatedAsync();
        }

        foreach (var roleName in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Created role {Role}", roleName);
            }
        }

        const string adminEmail = "gkk2283@gmail.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Admin",
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                EmployeeId = "ADM001"
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create admin: {Errors}", string.Join(",", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
            logger.LogInformation("Seeded default admin user.");
        }
        else if (!admin.IsActive)
        {
            admin.IsActive = true;
            await userManager.UpdateAsync(admin);
        }

        // Ensure profiles exist for all users with roles
        await EnsureAllRoleProfilesAsync(context, userManager, logger);

        await EnsureRoleBasedEmployeeIdsAsync(context, userManager, logger);
    }

    private static async Task EnsureAllRoleProfilesAsync(Data.ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var roles = Enum.GetValues<UserRole>();
        foreach (var role in roles)
        {
            if (role == UserRole.Admin) continue; // Admin has no profile

            var roleId = await context.Roles.Where(r => r.Name == role.ToString()).Select(r => r.Id).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(roleId)) continue;

            var roleUserIds = await context.UserRoles.Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId).ToListAsync();
            if (!roleUserIds.Any()) continue;

            IQueryable<string> existingUserIds = role switch
            {
                UserRole.Consultant => context.Consultants.Select(c => c.UserId),
                UserRole.SalesRecruiter => context.SalesRecruiters.Select(s => s.UserId),
                UserRole.Management => context.ManagementUsers.Select(m => m.UserId),
                _ => throw new InvalidOperationException()
            };

            var missingUserIds = roleUserIds.Except(await existingUserIds.ToListAsync()).ToList();
            if (!missingUserIds.Any()) continue;

            var users = await userManager.Users.Where(u => missingUserIds.Contains(u.Id)).ToListAsync();
            foreach (var user in users)
            {
                switch (role)
                {
                    case UserRole.Consultant:
                        context.Consultants.Add(new Consultant
                        {
                            UserId = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email ?? string.Empty,
                            PhoneNumber = user.PhoneNumber,
                            Status = "Active",
                            CreatedAt = DateTime.UtcNow
                        });
                        break;
                    case UserRole.SalesRecruiter:
                        context.SalesRecruiters.Add(new SalesRecruiter
                        {
                            UserId = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email ?? string.Empty,
                            PhoneNumber = user.PhoneNumber,
                            Status = "Active",
                            CreatedAt = DateTime.UtcNow
                        });
                        break;
                    case UserRole.Management:
                        context.ManagementUsers.Add(new ManagementUser
                        {
                            UserId = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email ?? string.Empty,
                            PhoneNumber = user.PhoneNumber,
                            Status = "Active",
                            CreatedAt = DateTime.UtcNow
                        });
                        break;
                }
                logger.LogInformation("Created missing {Role} profile for user {UserId}", role, user.Id);
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Saved missing role profiles.");
        }
    }

    private static async Task EnsureRoleBasedEmployeeIdsAsync(
        Data.ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var users = await context.Users.ToListAsync();
        if (!users.Any()) return;

        var nextNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in Enum.GetValues<UserRole>())
        {
            var prefix = EmployeeIdGenerator.GetPrefix(role);
            var maxForPrefix = users
                .Where(u => !string.IsNullOrWhiteSpace(u.EmployeeId) && u.EmployeeId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(u => int.TryParse(u.EmployeeId.Substring(3), out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            nextNumbers[prefix] = maxForPrefix + 1;
        }

        var changed = 0;
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = ResolvePrimaryRole(roles);
            var prefix = EmployeeIdGenerator.GetPrefix(role);

            if (!IsIdForPrefix(user.EmployeeId, prefix))
            {
                user.EmployeeId = EmployeeIdGenerator.Build(prefix, nextNumbers[prefix]++);
                changed++;
            }
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Updated role-based EmployeeIds for {Count} users", changed);
        }
    }

    private static bool IsIdForPrefix(string? employeeId, string prefix)
    {
        if (string.IsNullOrWhiteSpace(employeeId)) return false;
        if (!employeeId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        return employeeId.Length > 3 && int.TryParse(employeeId.Substring(3), out _);
    }

    private static UserRole ResolvePrimaryRole(IList<string> roles)
    {
        if (roles.Contains(UserRole.Admin.ToString(), StringComparer.OrdinalIgnoreCase)) return UserRole.Admin;
        if (roles.Contains(UserRole.Management.ToString(), StringComparer.OrdinalIgnoreCase)) return UserRole.Management;
        if (roles.Contains(UserRole.SalesRecruiter.ToString(), StringComparer.OrdinalIgnoreCase)) return UserRole.SalesRecruiter;
        return UserRole.Consultant;
    }
}
