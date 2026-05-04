using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ConsultancyManagement.Infrastructure.Data;

namespace ConsultancyManagement.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var consultantRoleId = await _db.Roles.Where(r => r.Name == UserRole.Consultant.ToString()).Select(r => r.Id).FirstOrDefaultAsync();
        var salesRoleId = await _db.Roles.Where(r => r.Name == UserRole.SalesRecruiter.ToString()).Select(r => r.Id).FirstOrDefaultAsync();
        var managementRoleId = await _db.Roles.Where(r => r.Name == UserRole.Management.ToString()).Select(r => r.Id).FirstOrDefaultAsync();

        var jobsFromDailyToday = await _db.DailyActivities
            .Where(d => d.ActivityDate >= today && d.ActivityDate < tomorrow)
            .SumAsync(d => (int?)d.JobsAppliedCount) ?? 0;
        var jobsFromApplicationsToday = await _db.JobApplications.CountAsync(j =>
            j.AppliedDate >= today && j.AppliedDate < tomorrow);

        return new AdminDashboardDto
        {
            TotalConsultants = string.IsNullOrEmpty(consultantRoleId) ? 0 : await ActiveUsersInRoleCountAsync(consultantRoleId),
            TotalSalesRecruiters = string.IsNullOrEmpty(salesRoleId) ? 0 : await ActiveUsersInRoleCountAsync(salesRoleId),
            TotalManagementUsers = string.IsNullOrEmpty(managementRoleId) ? 0 : await ActiveUsersInRoleCountAsync(managementRoleId),
            // Jobs applied: all consultants’ daily activity totals plus standalone job-application rows (no overlap assumed).
            TodayApplications = jobsFromDailyToday + jobsFromApplicationsToday,
            TodaySubmissions = await _db.Submissions.CountAsync(s => s.SubmissionDate >= today && s.SubmissionDate < tomorrow),
            PendingDocuments = await _db.Documents.CountAsync(d => d.Status == "Pending")
        };
    }

    public async Task<string?> PreviewNextEmployeeIdAsync(string roleName)
    {
        if (!ValidationHelper.TryParseRole(roleName, out var roleEnum))
            return null;
        return await GetNextEmployeeIdAsync(roleEnum);
    }

    public async Task<IReadOnlyList<AdminUserListDto>> GetUsersAsync()
    {
        var users = await _userManager.Users.AsNoTracking().Where(u => !u.IsDeleted).OrderBy(u => u.Email).ToListAsync();
        var list = new List<AdminUserListDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            list.Add(new AdminUserListDto
            {
                Id = u.EmployeeId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                Roles = roles.ToList(),
                IsActive = u.IsActive,
                IsDeleted = false
            });
        }
        return list;
    }

    public async Task<AdminUserDetailDto?> GetUserByIdAsync(string employeeId)
    {
        var u = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (u is null) return null;
        var roles = await _userManager.GetRolesAsync(u);
        return new AdminUserDetailDto
        {
            Id = u.EmployeeId,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email ?? string.Empty,
            PhoneNumber = u.PhoneNumber,
            Roles = roles.ToList(),
            IsActive = u.IsActive,
            IsDeleted = u.IsDeleted,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            DeletedAt = u.DeletedAt
        };
    }

    public async Task<(bool Success, string? Error, string? Id)> CreateUserAsync(CreateAdminUserRequestDto dto)
    {
        if (!ValidationHelper.IsValidEmail(dto.Email)) return (false, "A valid email is required.", null);
        if (string.IsNullOrWhiteSpace(dto.Password)) return (false, "Password is required.", null);
        if (string.IsNullOrWhiteSpace(dto.Role)) return (false, "Role is required.", null);
        if (!ValidationHelper.TryParseRole(dto.Role, out var roleEnum)) return (false, "Role must exist.", null);

        var normCreate = _userManager.NormalizeEmail(dto.Email.Trim());
        if (await _userManager.Users.AnyAsync(u => !u.IsDeleted && u.NormalizedEmail == normCreate))
            return (false, "A user with this email already exists.", null);

        string assignedEmployeeId;
        if (!string.IsNullOrWhiteSpace(dto.EmployeeId))
        {
            if (!ValidationHelper.TryValidateEmployeeIdForRole(dto.EmployeeId, roleEnum, out var empErr))
                return (false, empErr, null);
            assignedEmployeeId = dto.EmployeeId.Trim().ToUpperInvariant();
            if (await _userManager.Users.AnyAsync(u => u.EmployeeId == assignedEmployeeId))
                return (false, "That employee id is already in use.", null);
        }
        else
        {
            assignedEmployeeId = await GetNextEmployeeIdAsync(roleEnum);
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            EmailConfirmed = true,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            PhoneNumber = dto.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmployeeId = assignedEmployeeId
        };
        var r = await _userManager.CreateAsync(user, dto.Password);
        if (!r.Succeeded) return (false, string.Join(" ", r.Errors.Select(e => e.Description)), null);

        await _userManager.AddToRoleAsync(user, roleEnum.ToString());
        await EnsureRoleProfileAsync(user, roleEnum);
        return (true, null, user.EmployeeId);
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(string employeeId, UpdateAdminUserRequestDto dto)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user is null) return (false, "User not found.");
        if (user.IsDeleted) return (false, "User account is archived.");
        if (!ValidationHelper.IsValidEmail(dto.Email)) return (false, "A valid email is required.");
        var normalizedEmail = _userManager.NormalizeEmail(dto.Email.Trim());
        var duplicate = await _userManager.Users.AnyAsync(u =>
            u.Id != user.Id && !u.IsDeleted && u.NormalizedEmail == normalizedEmail);
        if (duplicate) return (false, "A user with this email already exists.");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Email = dto.Email.Trim();
        user.UserName = dto.Email.Trim();
        user.PhoneNumber = dto.PhoneNumber;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded) return (false, string.Join(" ", update.Errors.Select(e => e.Description)));

        var currentRoles = await _userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(dto.Roles).ToList();
        var toAdd = dto.Roles.Except(currentRoles).Where(r => ValidationHelper.TryParseRole(r, out _)).ToList();
        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded) return (false, string.Join(" ", removeResult.Errors.Select(e => e.Description)));
        }

        if (toAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded) return (false, string.Join(" ", addResult.Errors.Select(e => e.Description)));
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);
        await SyncUserProfilesAsync(user, updatedRoles);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(string employeeId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user is null) return (false, "User not found.");
        if (user.IsDeleted) return (false, "User already removed.");

        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == user.Id);
        var recruiter = await _db.SalesRecruiters.FirstOrDefaultAsync(s => s.UserId == user.Id);
        var management = await _db.ManagementUsers.FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (consultant is not null)
        {
            consultant.Status = "Inactive";
            consultant.UpdatedAt = DateTime.UtcNow;
        }

        if (recruiter is not null)
        {
            recruiter.Status = "Inactive";
            recruiter.UpdatedAt = DateTime.UtcNow;
        }

        if (management is not null)
        {
            management.Status = "Inactive";
            management.UpdatedAt = DateTime.UtcNow;
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync();

        var stamp = ".deleted." + DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture) + "." +
                    Guid.NewGuid().ToString("N")[..8];
        var baseEmail = user.Email ?? user.UserName ?? "user";
        user.Email = baseEmail + stamp;
        user.UserName = user.Email;
        user.NormalizedEmail = _userManager.NormalizeEmail(user.Email);
        user.NormalizedUserName = _userManager.NormalizeName(user.UserName);
        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    public async Task<IReadOnlyList<RoleListDto>> GetRolesAsync()
    {
        return await _db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleListDto { Name = r.Name ?? string.Empty })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateConsultantAsync(CreateConsultantRequestDto dto)
    {
        var user = await ResolveIdentityUserAsync(dto.UserId);
        if (user is null) return (false, "Consultant user not found.", null);
        if (await _db.Consultants.AnyAsync(c => c.UserId == user.Id))
            return (false, "Consultant profile already exists for this user.", null);

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.Consultant.ToString()))
            return (false, "User must have Consultant role.", null);

        var c = new Consultant
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            VisaStatus = dto.VisaStatus,
            Technology = dto.Technology,
            SkillsNotes = dto.SkillsNotes,
            ExperienceYears = dto.ExperienceYears,
            CurrentLocation = dto.CurrentLocation,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow
        };
        _db.Consultants.Add(c);
        await _db.SaveChangesAsync();
        return (true, null, c.Id);
    }

    public async Task<IReadOnlyList<ConsultantListDto>> GetConsultantsAsync()
    {
        await EnsureRoleProfilesAsync(UserRole.Consultant);

        return await _db.Consultants.AsNoTracking()
            .OrderBy(c => c.LastName)
            .Select(c => new ConsultantListDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserEmployeeId = c.User.EmployeeId,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Technology = c.Technology,
                SkillsNotes = c.SkillsNotes,
                VisaStatus = c.VisaStatus,
                ExperienceYears = c.ExperienceYears,
                CurrentLocation = c.CurrentLocation,
                Status = c.Status
            }).ToListAsync();
    }

    public async Task<ConsultantListDto?> GetConsultantByEmployeeIdAsync(string employeeId)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user is null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.Consultant.ToString())) return null;

        var consultant = await _db.Consultants.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == user.Id);
        if (consultant is null)
        {
            return await CreateConsultantProfileFromUserAsync(user);
        }

        return new ConsultantListDto
        {
            Id = consultant.Id,
            UserId = consultant.UserId,
            UserEmployeeId = user.EmployeeId,
            FirstName = consultant.FirstName,
            LastName = consultant.LastName,
            Email = consultant.Email,
            Technology = consultant.Technology,
            SkillsNotes = consultant.SkillsNotes,
            VisaStatus = consultant.VisaStatus,
            ExperienceYears = consultant.ExperienceYears,
            CurrentLocation = consultant.CurrentLocation,
            Status = consultant.Status
        };
    }

    public async Task<(bool Success, string? Error)> UpdateConsultantAsync(string employeeId, CreateConsultantRequestDto dto)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user is null) return (false, "Consultant not found.");
        if (user.IsDeleted) return (false, "Consultant account is archived.");
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.Consultant.ToString())) return (false, "Consultant not found.");

        var consultant = await _db.Consultants.FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (consultant is null)
        {
            consultant = new Consultant
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.Consultants.Add(consultant);
        }

        consultant.FirstName = dto.FirstName;
        consultant.LastName = dto.LastName;
        consultant.Email = dto.Email;
        consultant.PhoneNumber = dto.PhoneNumber;
        consultant.VisaStatus = dto.VisaStatus;
        consultant.Technology = dto.Technology;
        consultant.SkillsNotes = dto.SkillsNotes;
        consultant.ExperienceYears = dto.ExperienceYears;
        consultant.CurrentLocation = dto.CurrentLocation;
        consultant.Status = dto.Status;
        consultant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateSalesRecruiterAsync(CreateSalesRecruiterRequestDto dto)
    {
        var user = await ResolveIdentityUserAsync(dto.UserId);
        if (user is null) return (false, "User not found.", null);
        if (await _db.SalesRecruiters.AnyAsync(s => s.UserId == user.Id))
            return (false, "Sales recruiter profile already exists.", null);
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.SalesRecruiter.ToString()))
            return (false, "User must have SalesRecruiter role.", null);

        var s = new SalesRecruiter
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow
        };
        _db.SalesRecruiters.Add(s);
        await _db.SaveChangesAsync();
        return (true, null, s.Id);
    }

    public async Task<IReadOnlyList<SalesRecruiterListDto>> GetSalesRecruitersAsync()
    {
        await EnsureRoleProfilesAsync(UserRole.SalesRecruiter);

        return await _db.SalesRecruiters.AsNoTracking()
            .OrderBy(s => s.LastName)
            .Select(s => new SalesRecruiterListDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserEmployeeId = s.User.EmployeeId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                PhoneNumber = s.PhoneNumber,
                Status = s.Status,
                AssignedConsultantsCount = s.ConsultantAssignments.Count(a => a.IsActive)
            }).ToListAsync();
    }

    public async Task<SalesRecruiterListDto?> GetSalesRecruiterByEmployeeIdAsync(string employeeId)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user is null) return null;
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.SalesRecruiter.ToString())) return null;

        var recruiter = await _db.SalesRecruiters.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (recruiter is null)
        {
            return await CreateSalesRecruiterProfileFromUserAsync(user);
        }

        return new SalesRecruiterListDto
        {
            Id = recruiter.Id,
            UserId = recruiter.UserId,
            UserEmployeeId = user.EmployeeId,
            FirstName = recruiter.FirstName,
            LastName = recruiter.LastName,
            Email = recruiter.Email,
            PhoneNumber = recruiter.PhoneNumber,
            Status = recruiter.Status,
            AssignedConsultantsCount = recruiter.ConsultantAssignments.Count(a => a.IsActive)
        };
    }

    public async Task<(bool Success, string? Error)> UpdateSalesRecruiterAsync(string employeeId, CreateSalesRecruiterRequestDto dto)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user is null) return (false, "Sales recruiter not found.");
        if (user.IsDeleted) return (false, "Sales recruiter account is archived.");
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.SalesRecruiter.ToString())) return (false, "Sales recruiter not found.");

        var recruiter = await _db.SalesRecruiters.FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (recruiter is null)
        {
            recruiter = new SalesRecruiter
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.SalesRecruiters.Add(recruiter);
        }

        recruiter.FirstName = dto.FirstName;
        recruiter.LastName = dto.LastName;
        recruiter.Email = dto.Email;
        recruiter.PhoneNumber = dto.PhoneNumber;
        recruiter.Status = dto.Status;
        recruiter.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateManagementUserAsync(CreateManagementUserRequestDto dto)
    {
        var user = await ResolveIdentityUserAsync(dto.UserId);
        if (user is null) return (false, "User not found.", null);
        if (await _db.ManagementUsers.AnyAsync(m => m.UserId == user.Id))
            return (false, "Management profile already exists.", null);
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.Management.ToString()))
            return (false, "User must have Management role.", null);

        var m = new ManagementUser
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Department = dto.Department,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow
        };
        _db.ManagementUsers.Add(m);
        await _db.SaveChangesAsync();
        return (true, null, m.Id);
    }

    public async Task<IReadOnlyList<ManagementUserListDto>> GetManagementUsersAsync()
    {
        await EnsureRoleProfilesAsync(UserRole.Management);

        return await _db.ManagementUsers.AsNoTracking()
            .OrderBy(m => m.LastName)
            .Select(m => new ManagementUserListDto
            {
                Id = m.Id,
                UserId = m.UserId,
                UserEmployeeId = m.User.EmployeeId,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                Department = m.Department,
                Status = m.Status
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateManagementUserAsync(string employeeId, CreateManagementUserRequestDto dto)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user is null) return (false, "Management user not found.");
        if (user.IsDeleted) return (false, "Management user account is archived.");
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(UserRole.Management.ToString())) return (false, "Management user not found.");

        var mg = await _db.ManagementUsers.FirstOrDefaultAsync(x => x.UserId == user.Id);
        if (mg is null)
        {
            mg = new ManagementUser
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.ManagementUsers.Add(mg);
        }

        mg.FirstName = dto.FirstName;
        mg.LastName = dto.LastName;
        mg.Email = dto.Email;
        mg.PhoneNumber = dto.PhoneNumber;
        mg.Department = dto.Department;
        mg.Status = dto.Status;
        mg.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task EnsureRoleProfileAsync(ApplicationUser user, UserRole role)
    {
        switch (role)
        {
            case UserRole.Consultant:
                if (!await _db.Consultants.AnyAsync(c => c.UserId == user.Id))
                {
                    _db.Consultants.Add(new Consultant
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                break;
            case UserRole.SalesRecruiter:
                if (!await _db.SalesRecruiters.AnyAsync(s => s.UserId == user.Id))
                {
                    _db.SalesRecruiters.Add(new SalesRecruiter
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                break;
            case UserRole.Management:
                if (!await _db.ManagementUsers.AnyAsync(m => m.UserId == user.Id))
                {
                    _db.ManagementUsers.Add(new ManagementUser
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                break;
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task SyncUserProfilesAsync(ApplicationUser user, IList<string> roles)
    {
        var activeRoles = roles
            .Where(r => ValidationHelper.TryParseRole(r, out _))
            .Select(r => Enum.Parse<UserRole>(r, ignoreCase: true))
            .ToHashSet();

        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == user.Id);
        var salesRecruiter = await _db.SalesRecruiters.FirstOrDefaultAsync(s => s.UserId == user.Id);
        var management = await _db.ManagementUsers.FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (activeRoles.Contains(UserRole.Consultant))
        {
            if (consultant is null)
            {
                _db.Consultants.Add(new Consultant
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (consultant is not null)
        {
            _db.Consultants.Remove(consultant);
        }

        if (activeRoles.Contains(UserRole.SalesRecruiter))
        {
            if (salesRecruiter is null)
            {
                _db.SalesRecruiters.Add(new SalesRecruiter
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (salesRecruiter is not null)
        {
            _db.SalesRecruiters.Remove(salesRecruiter);
        }

        if (activeRoles.Contains(UserRole.Management))
        {
            if (management is null)
            {
                _db.ManagementUsers.Add(new ManagementUser
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (management is not null)
        {
            _db.ManagementUsers.Remove(management);
        }

        if (consultant is not null)
        {
            consultant.FirstName = user.FirstName;
            consultant.LastName = user.LastName;
            consultant.Email = user.Email ?? string.Empty;
            consultant.PhoneNumber = user.PhoneNumber;
        }

        if (salesRecruiter is not null)
        {
            salesRecruiter.FirstName = user.FirstName;
            salesRecruiter.LastName = user.LastName;
            salesRecruiter.Email = user.Email ?? string.Empty;
            salesRecruiter.PhoneNumber = user.PhoneNumber;
        }

        if (management is not null)
        {
            management.FirstName = user.FirstName;
            management.LastName = user.LastName;
            management.Email = user.Email ?? string.Empty;
            management.PhoneNumber = user.PhoneNumber;
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task<ConsultantListDto?> CreateConsultantProfileFromUserAsync(ApplicationUser user)
    {
        var consultant = new Consultant
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _db.Consultants.Add(consultant);
        await _db.SaveChangesAsync();

        return new ConsultantListDto
        {
            Id = consultant.Id,
            UserId = consultant.UserId,
            UserEmployeeId = user.EmployeeId,
            FirstName = consultant.FirstName,
            LastName = consultant.LastName,
            Email = consultant.Email,
            Technology = consultant.Technology,
            SkillsNotes = consultant.SkillsNotes,
            VisaStatus = consultant.VisaStatus,
            ExperienceYears = consultant.ExperienceYears,
            CurrentLocation = consultant.CurrentLocation,
            Status = consultant.Status
        };
    }

    private async Task<SalesRecruiterListDto?> CreateSalesRecruiterProfileFromUserAsync(ApplicationUser user)
    {
        var recruiter = new SalesRecruiter
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _db.SalesRecruiters.Add(recruiter);
        await _db.SaveChangesAsync();

        return new SalesRecruiterListDto
        {
            Id = recruiter.Id,
            UserId = recruiter.UserId,
            UserEmployeeId = user.EmployeeId,
            FirstName = recruiter.FirstName,
            LastName = recruiter.LastName,
            Email = recruiter.Email,
            PhoneNumber = recruiter.PhoneNumber,
            Status = recruiter.Status,
            AssignedConsultantsCount = 0
        };
    }

    private async Task EnsureRoleProfilesAsync(UserRole role)
    {
        var roleId = await _db.Roles.Where(r => r.Name == role.ToString()).Select(r => r.Id).FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(roleId)) return;

        IQueryable<string> roleUserIds = _db.UserRoles.Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId);
        IQueryable<string> existingUserIds = role switch
        {
            UserRole.Consultant => _db.Consultants.Select(c => c.UserId),
            UserRole.SalesRecruiter => _db.SalesRecruiters.Select(s => s.UserId),
            UserRole.Management => _db.ManagementUsers.Select(m => m.UserId),
            _ => Enumerable.Empty<string>().AsQueryable()
        };

        var missingUserIds = await roleUserIds.Except(existingUserIds).ToListAsync();
        if (!missingUserIds.Any()) return;

        var users = await _userManager.Users.Where(u => missingUserIds.Contains(u.Id) && !u.IsDeleted).ToListAsync();
        foreach (var user in users)
        {
            switch (role)
            {
                case UserRole.Consultant:
                    _db.Consultants.Add(new Consultant
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
                    _db.SalesRecruiters.Add(new SalesRecruiter
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
                    _db.ManagementUsers.Add(new ManagementUser
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
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task<ApplicationUser?> ResolveIdentityUserAsync(string idOrEmployeeId)
    {
        if (string.IsNullOrWhiteSpace(idOrEmployeeId)) return null;
        var key = idOrEmployeeId.Trim();

        var byId = await _userManager.FindByIdAsync(key);
        if (byId is not null && !byId.IsDeleted) return byId;

        return await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == key && !u.IsDeleted);
    }

    private async Task<int> ActiveUsersInRoleCountAsync(string roleId) =>
        await (from ur in _db.UserRoles
            join u in _db.Users on ur.UserId equals u.Id
            where ur.RoleId == roleId && !u.IsDeleted
            select ur).CountAsync();

    private async Task<string> GetNextEmployeeIdAsync(UserRole role)
    {
        var prefix = EmployeeIdGenerator.GetPrefix(role);
        var ids = await _userManager.Users
            .Where(u => !string.IsNullOrEmpty(u.EmployeeId) && u.EmployeeId.StartsWith(prefix))
            .Select(u => u.EmployeeId)
            .ToListAsync();

        var nextNumber = 1;
        if (ids.Count > 0)
        {
            var max = ids
                .Select(id => int.TryParse(id.Substring(3), out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();
            nextNumber = max + 1;
        }

        return EmployeeIdGenerator.Build(prefix, nextNumber);
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateAssignmentAsync(CreateAssignmentRequestDto dto)
    {
        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.Id == dto.ConsultantId);
        if (consultant is null) return (false, "Consultant must exist before assigning.", null);
        var sales = await _db.SalesRecruiters.FirstOrDefaultAsync(s => s.Id == dto.SalesRecruiterId);
        if (sales is null) return (false, "Sales recruiter must exist before assigning.", null);

        var dup = await _db.ConsultantSalesAssignments.AnyAsync(a =>
            a.ConsultantId == dto.ConsultantId && a.SalesRecruiterId == dto.SalesRecruiterId && a.IsActive);
        if (dup) return (false, "Duplicate assignment is not allowed for the same consultant and sales recruiter.", null);

        var a = new ConsultantSalesAssignment
        {
            ConsultantId = dto.ConsultantId,
            SalesRecruiterId = dto.SalesRecruiterId,
            StartDate = dto.StartDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.ConsultantSalesAssignments.Add(a);
        await _db.SaveChangesAsync();
        return (true, null, a.Id);
    }

    public async Task<IReadOnlyList<AssignmentListDto>> GetAssignmentsAsync()
    {
        return await _db.ConsultantSalesAssignments.AsNoTracking()
            .OrderByDescending(a => a.StartDate)
            .Select(a => new AssignmentListDto
            {
                Id = a.Id,
                ConsultantId = a.ConsultantId,
                ConsultantName = a.Consultant.FirstName + " " + a.Consultant.LastName,
                SalesRecruiterId = a.SalesRecruiterId,
                SalesRecruiterName = a.SalesRecruiter.FirstName + " " + a.SalesRecruiter.LastName,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                IsActive = a.IsActive
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateAssignmentAsync(int id, UpdateAssignmentRequestDto dto)
    {
        var a = await _db.ConsultantSalesAssignments.FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return (false, "Assignment not found.");
        a.EndDate = dto.EndDate;
        a.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateSalesManagementAssignmentAsync(CreateSalesManagementAssignmentRequestDto dto)
    {
        var sales = await _db.SalesRecruiters.FirstOrDefaultAsync(s => s.Id == dto.SalesRecruiterId);
        if (sales is null) return (false, "Sales recruiter not found.", null);
        var mgmt = await _db.ManagementUsers.FirstOrDefaultAsync(m => m.Id == dto.ManagementUserId);
        if (mgmt is null) return (false, "Management user not found.", null);

        var dup = await _db.SalesManagementAssignments.AnyAsync(a =>
            a.SalesRecruiterId == dto.SalesRecruiterId && a.ManagementUserId == dto.ManagementUserId && a.IsActive);
        if (dup) return (false, "An active assignment already exists for this sales and management pair.", null);

        var a = new SalesManagementAssignment
        {
            SalesRecruiterId = dto.SalesRecruiterId,
            ManagementUserId = dto.ManagementUserId,
            StartDate = dto.StartDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.SalesManagementAssignments.Add(a);
        await _db.SaveChangesAsync();
        return (true, null, a.Id);
    }

    public async Task<IReadOnlyList<SalesManagementAssignmentListDto>> GetSalesManagementAssignmentsAsync()
    {
        return await _db.SalesManagementAssignments.AsNoTracking()
            .OrderByDescending(a => a.StartDate)
            .Select(a => new SalesManagementAssignmentListDto
            {
                Id = a.Id,
                SalesRecruiterId = a.SalesRecruiterId,
                SalesRecruiterName = a.SalesRecruiter.FirstName + " " + a.SalesRecruiter.LastName,
                ManagementUserId = a.ManagementUserId,
                ManagementUserName = a.ManagementUser.FirstName + " " + a.ManagementUser.LastName,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                IsActive = a.IsActive
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateSalesManagementAssignmentAsync(int id, UpdateAssignmentRequestDto dto)
    {
        var a = await _db.SalesManagementAssignments.FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return (false, "Assignment not found.");
        a.EndDate = dto.EndDate;
        a.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
