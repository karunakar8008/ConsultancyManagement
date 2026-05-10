using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ConsultancyManagement.Infrastructure.Services;

namespace ConsultancyManagement.Infrastructure.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger, IHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<Data.ApplicationDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            if (environment.IsDevelopment())
            {
                logger.LogWarning(ex,
                    "Database.MigrateAsync failed; falling back to EnsureCreated (development only). For production run: dotnet ef database update");
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                logger.LogCritical(ex,
                    "Database.MigrateAsync failed. Apply migrations before starting in {Environment}. Connection string must point at the correct database.",
                    environment.EnvironmentName);
                throw;
            }
        }

        foreach (var roleName in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Created role {Role}", roleName);
            }
        }

        if (!await context.Organizations.AnyAsync())
        {
            context.Organizations.Add(new Organization
            {
                Name = "Default organization",
                Slug = "default",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default organization (slug: default).");
        }

        var defaultOrgForActive = await context.Organizations.FirstOrDefaultAsync(o => o.Slug == "default");
        if (defaultOrgForActive is not null && !defaultOrgForActive.IsActive)
        {
            defaultOrgForActive.IsActive = true;
            await context.SaveChangesAsync();
            logger.LogInformation("Reactivated default organization (slug: default) for sign-in.");
        }

        var defaultOrgId = await context.Organizations.AsNoTracking()
            .Where(o => o.Slug == "default")
            .Select(o => o.Id)
            .FirstAsync();

        const string adminEmail = "gkk2283@gmail.com";
        const string adminSeedPassword = "Enzo@0324";

        var normAdmin = userManager.NormalizeEmail(adminEmail);
        var admin = await context.Users.FirstOrDefaultAsync(u =>
            u.OrganizationId == defaultOrgId && u.NormalizedEmail == normAdmin);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                OrganizationId = defaultOrgId,
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

            var result = await userManager.CreateAsync(admin, adminSeedPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create admin: {Errors}", string.Join(",", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
            await userManager.AddToRoleAsync(admin, UserRole.PlatformAdmin.ToString());
            logger.LogInformation("Seeded default admin user (Admin + PlatformAdmin).");
            var adminFresh = await userManager.FindByIdAsync(admin.Id);
            if (adminFresh != null)
                await ClearIdentityLockoutForSeededUserAsync(userManager, adminFresh, logger);
        }
        else
        {
            if (admin.OrganizationId != defaultOrgId)
            {
                admin.OrganizationId = defaultOrgId;
                await userManager.UpdateAsync(admin);
            }
            if (!admin.IsActive)
            {
                admin.IsActive = true;
                await userManager.UpdateAsync(admin);
            }

            var roles = await userManager.GetRolesAsync(admin);
            if (!roles.Contains(UserRole.PlatformAdmin.ToString()))
            {
                await userManager.AddToRoleAsync(admin, UserRole.PlatformAdmin.ToString());
                logger.LogInformation("Granted PlatformAdmin to existing admin.");
            }

            // One-time migration: seeded admin previously used Admin@123.
            if (await userManager.CheckPasswordAsync(admin, "Admin@123"))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                var reset = await userManager.ResetPasswordAsync(admin, token, adminSeedPassword);
                if (reset.Succeeded)
                    logger.LogInformation("Updated seeded admin password from legacy default.");
                else
                    logger.LogWarning("Could not migrate admin password: {Errors}",
                        string.Join(",", reset.Errors.Select(e => e.Description)));
            }

            // Local dev: if the DB password does not match the seed constant, align it so curl/UI login works.
            if (environment.IsDevelopment()
                && !await userManager.CheckPasswordAsync(admin, adminSeedPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(admin);
                var reset = await userManager.ResetPasswordAsync(admin, token, adminSeedPassword);
                if (reset.Succeeded)
                    logger.LogWarning(
                        "Development: reset seeded admin ({Email}) password to match DbSeeder so local login succeeds.",
                        adminEmail);
                else
                    logger.LogWarning("Development: could not align admin password: {Errors}",
                        string.Join(",", reset.Errors.Select(e => e.Description)));
            }

            var adminFresh = await userManager.FindByIdAsync(admin.Id);
            if (adminFresh != null)
                await ClearIdentityLockoutForSeededUserAsync(userManager, adminFresh, logger);
        }

        const string platformAdminEmail = "admin@consultancymanagementsolutions.com";
        const string platformAdminSeedPassword = "Enzobuddy@03240407";

        var normPlatformAdmin = userManager.NormalizeEmail(platformAdminEmail);
        var platformAdmin = await context.Users.FirstOrDefaultAsync(u =>
            u.OrganizationId == defaultOrgId && u.NormalizedEmail == normPlatformAdmin);
        if (platformAdmin is null)
        {
            platformAdmin = new ApplicationUser
            {
                OrganizationId = defaultOrgId,
                UserName = platformAdminEmail,
                Email = platformAdminEmail,
                EmailConfirmed = true,
                FirstName = "Platform",
                LastName = "Admin",
                IsActive = true,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow,
                EmployeeId = "ADM002"
            };

            var result = await userManager.CreateAsync(platformAdmin, platformAdminSeedPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create platform admin: {Errors}",
                    string.Join(",", result.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(platformAdmin, UserRole.Admin.ToString());
            await userManager.AddToRoleAsync(platformAdmin, UserRole.PlatformAdmin.ToString());
            logger.LogInformation("Seeded platform admin user ({Email}, Admin + PlatformAdmin).", platformAdminEmail);
            var platformFresh = await userManager.FindByIdAsync(platformAdmin.Id);
            if (platformFresh != null)
                await ClearIdentityLockoutForSeededUserAsync(userManager, platformFresh, logger);
        }
        else
        {
            if (platformAdmin.OrganizationId != defaultOrgId)
            {
                platformAdmin.OrganizationId = defaultOrgId;
                await userManager.UpdateAsync(platformAdmin);
            }

            if (!platformAdmin.IsActive)
            {
                platformAdmin.IsActive = true;
                await userManager.UpdateAsync(platformAdmin);
            }

            var platformRoles = await userManager.GetRolesAsync(platformAdmin);
            if (!platformRoles.Contains(UserRole.Admin.ToString()))
            {
                await userManager.AddToRoleAsync(platformAdmin, UserRole.Admin.ToString());
                logger.LogInformation("Granted Admin to existing platform admin user.");
            }

            if (!platformRoles.Contains(UserRole.PlatformAdmin.ToString()))
            {
                await userManager.AddToRoleAsync(platformAdmin, UserRole.PlatformAdmin.ToString());
                logger.LogInformation("Granted PlatformAdmin to existing platform admin user.");
            }

            if (environment.IsDevelopment()
                && !await userManager.CheckPasswordAsync(platformAdmin, platformAdminSeedPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(platformAdmin);
                var reset = await userManager.ResetPasswordAsync(platformAdmin, token, platformAdminSeedPassword);
                if (reset.Succeeded)
                    logger.LogWarning(
                        "Development: reset platform admin ({Email}) password to match DbSeeder so local login succeeds.",
                        platformAdminEmail);
                else
                    logger.LogWarning("Development: could not align platform admin password: {Errors}",
                        string.Join(",", reset.Errors.Select(e => e.Description)));
            }

            var platformFresh = await userManager.FindByIdAsync(platformAdmin.Id);
            if (platformFresh != null)
                await ClearIdentityLockoutForSeededUserAsync(userManager, platformFresh, logger);
        }

        var usersMissingOrg = await context.Users.Where(u => u.OrganizationId == 0).ToListAsync();
        foreach (var u in usersMissingOrg)
        {
            u.OrganizationId = defaultOrgId;
            await userManager.UpdateAsync(u);
        }

        await BackfillDenormalizedOrganizationIdsAsync(context, logger);

        // Ensure profiles exist for all users with roles
        await EnsureAllRoleProfilesAsync(context, userManager, logger);

        await EnsureRoleBasedEmployeeIdsAsync(context, userManager, logger);
    }

    /// <summary>
    /// Too many failed password attempts set Identity lockout; sign-in then fails even with the correct password.
    /// Cleared on startup for the two seeded operator accounts so local/dev login recovers without manual DB edits.
    /// </summary>
    private static async Task ClearIdentityLockoutForSeededUserAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        ILogger logger)
    {
        if (user.LockoutEnd is null && user.AccessFailedCount == 0) return;

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        logger.LogInformation("Cleared Identity lockout / failed-attempt state for seeded user {Email}.", user.Email);
    }

    private static async Task BackfillDenormalizedOrganizationIdsAsync(
        Data.ApplicationDbContext context,
        ILogger logger)
    {
        var defaultOrgId = await context.Organizations.AsNoTracking()
            .Where(o => o.Slug == "default")
            .Select(o => (int?)o.Id)
            .FirstOrDefaultAsync() ?? 0;
        if (defaultOrgId == 0) return;

        var userOrgs = await context.Users.AsNoTracking()
            .Select(u => new { u.Id, u.OrganizationId })
            .ToListAsync();
        var map = userOrgs.ToDictionary(x => x.Id, x => x.OrganizationId);

        var touched = false;
        foreach (var c in await context.Consultants.Where(x => x.OrganizationId == 0).ToListAsync())
        {
            c.OrganizationId = map.GetValueOrDefault(c.UserId, defaultOrgId);
            touched = true;
        }

        foreach (var s in await context.SalesRecruiters.Where(x => x.OrganizationId == 0).ToListAsync())
        {
            s.OrganizationId = map.GetValueOrDefault(s.UserId, defaultOrgId);
            touched = true;
        }

        foreach (var m in await context.ManagementUsers.Where(x => x.OrganizationId == 0).ToListAsync())
        {
            m.OrganizationId = map.GetValueOrDefault(m.UserId, defaultOrgId);
            touched = true;
        }

        foreach (var v in await context.Vendors.Where(x => x.OrganizationId == 0).ToListAsync())
        {
            if (v.SalesRecruiterId.HasValue)
            {
                v.OrganizationId = await context.SalesRecruiters.AsNoTracking()
                    .Where(s => s.Id == v.SalesRecruiterId)
                    .Select(s => s.OrganizationId)
                    .FirstOrDefaultAsync();
            }

            if (v.OrganizationId == 0 && v.LinkedConsultantId.HasValue)
            {
                v.OrganizationId = await context.Consultants.AsNoTracking()
                    .Where(c => c.Id == v.LinkedConsultantId)
                    .Select(c => c.OrganizationId)
                    .FirstOrDefaultAsync();
            }

            if (v.OrganizationId == 0) v.OrganizationId = defaultOrgId;
            touched = true;
        }

        if (touched)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Backfilled OrganizationId on profiles and vendors.");
        }
    }

    private static async Task EnsureAllRoleProfilesAsync(Data.ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var roles = Enum.GetValues<UserRole>();
        foreach (var role in roles)
        {
            if (role is UserRole.Admin or UserRole.PlatformAdmin) continue;

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
                            OrganizationId = user.OrganizationId,
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
                            OrganizationId = user.OrganizationId,
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
                            OrganizationId = user.OrganizationId,
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

        var changed = 0;
        foreach (var orgGroup in users.GroupBy(u => u.OrganizationId))
        {
            var nextNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var role in Enum.GetValues<UserRole>())
            {
                var prefix = EmployeeIdGenerator.GetPrefix(role);
                var maxForPrefix = orgGroup
                    .Where(u => !string.IsNullOrWhiteSpace(u.EmployeeId) && u.EmployeeId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(u => int.TryParse(u.EmployeeId.Substring(3), out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                nextNumbers[prefix] = maxForPrefix + 1;
            }

            foreach (var user in orgGroup)
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
        if (roles.Contains(UserRole.PlatformAdmin.ToString(), StringComparer.OrdinalIgnoreCase)) return UserRole.PlatformAdmin;
        if (roles.Contains(UserRole.Admin.ToString(), StringComparer.OrdinalIgnoreCase)) return UserRole.Admin;
        if (roles.Contains(UserRole.Management.ToString(), StringComparer.OrdinalIgnoreCase)) return UserRole.Management;
        if (roles.Contains(UserRole.SalesRecruiter.ToString(), StringComparer.OrdinalIgnoreCase)) return UserRole.SalesRecruiter;
        return UserRole.Consultant;
    }
}
