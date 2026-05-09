using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ConsultancyManagement.Infrastructure.Data;

namespace ConsultancyManagement.Infrastructure.Services;

public class ManagementPortalService : IManagementPortalService
{
    private static readonly HashSet<string> AllowedReviewStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Approved", "Rejected", "Re-check" };

    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _env;
    private readonly INotificationService _notifications;
    private readonly UserManager<ApplicationUser> _users;

    public ManagementPortalService(
        ApplicationDbContext db,
        IHostEnvironment env,
        INotificationService notifications,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _env = env;
        _notifications = notifications;
        _users = users;
    }

    private static int StoredAuthorityRank(string? authority) => authority switch
    {
        nameof(UserRole.Admin) => 4,
        nameof(UserRole.Management) => 3,
        nameof(UserRole.SalesRecruiter) => 2,
        _ => 0
    };

    private static int ReviewerRank(IList<string> roles)
    {
        if (roles.Contains(nameof(UserRole.Admin))) return 4;
        if (roles.Contains(nameof(UserRole.Management))) return 3;
        if (roles.Contains(nameof(UserRole.SalesRecruiter))) return 2;
        return 0;
    }

    private static string? PrimaryReviewerAuthority(IList<string> roles)
    {
        if (roles.Contains(nameof(UserRole.Admin))) return nameof(UserRole.Admin);
        if (roles.Contains(nameof(UserRole.Management))) return nameof(UserRole.Management);
        if (roles.Contains(nameof(UserRole.SalesRecruiter))) return nameof(UserRole.SalesRecruiter);
        return null;
    }

    public async Task<ManagementDashboardDto> GetDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        return new ManagementDashboardDto
        {
            TotalConsultants = await _db.Consultants.CountAsync(c => !c.User.IsDeleted),
            PendingOnboarding = await _db.OnboardingTasks.CountAsync(t => t.Status != "Completed"),
            PendingDocuments = await _db.Documents.CountAsync(d => d.Status == "Pending"),
            TotalSubmissions = await _db.Submissions.CountAsync(),
            InterviewsScheduled = await _db.Interviews.CountAsync(i => i.InterviewDate >= today && i.InterviewDate < tomorrow)
        };
    }

    public async Task<IReadOnlyList<SalesRecruiterListDto>> GetSalesRecruitersAsync()
    {
        return await _db.SalesRecruiters.AsNoTracking()
            .Where(s => !s.User.IsDeleted)
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

    public async Task<IReadOnlyList<ConsultantListDto>> GetConsultantsAsync()
    {
        return await _db.Consultants.AsNoTracking()
            .Where(c => !c.User.IsDeleted)
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

    public async Task<IReadOnlyList<DailyActivityDto>> GetConsultantActivitiesAsync(int consultantId)
    {
        return await _db.DailyActivities.AsNoTracking()
            .Where(d => d.ConsultantId == consultantId)
            .OrderByDescending(d => d.ActivityDate)
            .Select(d => new DailyActivityDto
            {
                Id = d.Id,
                ActivityDate = d.ActivityDate,
                JobsAppliedCount = d.JobsAppliedCount,
                VendorReachedOutCount = d.VendorReachedOutCount,
                VendorResponseCount = d.VendorResponseCount,
                SubmissionsCount = d.SubmissionsCount,
                InterviewCallsCount = d.InterviewCallsCount,
                Notes = d.Notes
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<SubmissionReportRowDto>> GetSubmissionsAsync()
    {
        return await _db.Submissions.AsNoTracking()
            .OrderByDescending(s => s.SubmissionDate)
            .Select(s => new SubmissionReportRowDto
            {
                Id = s.Id,
                ConsultantName = s.Consultant.FirstName + " " + s.Consultant.LastName,
                SalesRecruiterName = s.SalesRecruiter.FirstName + " " + s.SalesRecruiter.LastName,
                VendorName = s.Vendor.VendorName,
                JobTitle = s.JobTitle,
                SubmissionDate = s.SubmissionDate,
                Status = s.Status,
                Notes = s.Notes
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<OnboardingTaskDto>> GetOnboardingTasksAsync()
    {
        return await _db.OnboardingTasks.AsNoTracking()
            .OrderBy(t => t.DueDate)
            .Select(t => new OnboardingTaskDto
            {
                Id = t.Id,
                ConsultantId = t.ConsultantId,
                ConsultantName = t.Consultant.FirstName + " " + t.Consultant.LastName,
                TaskName = t.TaskName,
                Description = t.Description,
                Status = t.Status,
                DueDate = t.DueDate,
                CompletedDate = t.CompletedDate
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateOnboardingTaskAsync(CreateOnboardingTaskRequestDto dto)
    {
        if (!await _db.Consultants.AnyAsync(c => c.Id == dto.ConsultantId))
            return (false, "Consultant not found.", null);
        var t = new OnboardingTask
        {
            ConsultantId = dto.ConsultantId,
            TaskName = dto.TaskName,
            Description = dto.Description,
            Status = dto.Status,
            DueDate = dto.DueDate,
            CreatedAt = DateTime.UtcNow
        };
        _db.OnboardingTasks.Add(t);
        await _db.SaveChangesAsync();
        await _notifications.NotifyOnboardingTaskAssignedAsync(dto.ConsultantId, t.Id, dto.TaskName);
        return (true, null, t.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateOnboardingTaskAsync(int id, CreateOnboardingTaskRequestDto dto)
    {
        var t = await _db.OnboardingTasks.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return (false, "Task not found.");
        t.TaskName = dto.TaskName;
        t.Description = dto.Description;
        t.Status = dto.Status;
        t.DueDate = dto.DueDate;
        if (dto.Status == "Completed" && t.CompletedDate is null)
            t.CompletedDate = DateTime.UtcNow;
        t.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync()
    {
        var rows = await _db.Documents.AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.ConsultantId,
                Fn = d.Consultant.FirstName,
                Ln = d.Consultant.LastName,
                d.DocumentType,
                d.FileName,
                d.UploadedAt,
                d.Status,
                d.AdminReviewLockedAt,
                d.LastReviewAuthority
            })
            .ToListAsync();

        return rows.Select(d => new DocumentDto
        {
            Id = d.Id,
            ConsultantId = d.ConsultantId,
            ConsultantName = $"{d.Fn} {d.Ln}".Trim(),
            StorageFolder = ConsultantFolderNameHelper.BuildSegment(d.Fn, d.Ln, d.ConsultantId),
            DocumentType = d.DocumentType,
            FileName = d.FileName,
            UploadedAt = d.UploadedAt,
            Status = d.Status,
            LockedAfterAdminDecision = d.AdminReviewLockedAt.HasValue,
            LastReviewAuthority = d.LastReviewAuthority
        }).ToList();
    }

    public async Task<IReadOnlyList<ManagementFileCatalogItemDto>> GetFileCatalogAsync()
    {
        var list = new List<ManagementFileCatalogItemDto>();

        var docs = await _db.Documents.AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.ConsultantId,
                d.DocumentType,
                d.FileName,
                d.FilePath,
                d.UploadedAt,
                Fn = d.Consultant.FirstName,
                Ln = d.Consultant.LastName
            })
            .ToListAsync();
        foreach (var d in docs)
        {
            var consultantLabel = $"{d.Fn} {d.Ln}".Trim();
            var folder = ConsultantFolderNameHelper.BuildSegment(d.Fn, d.Ln, d.ConsultantId);
            list.Add(new ManagementFileCatalogItemDto
            {
                Kind = "Document",
                Id = d.Id,
                Title = d.DocumentType,
                Subtitle = "Consultant document",
                ConsultantName = consultantLabel,
                ConsultantStorageFolder = folder,
                FileName = d.FileName,
                HasFile = !string.IsNullOrWhiteSpace(d.FilePath),
                At = d.UploadedAt
            });
        }

        var vendors = await _db.Vendors.AsNoTracking()
            .Where(v => v.ContactProofFilePath != null && v.ContactProofFilePath != "")
            .Select(v => new
            {
                v.Id,
                v.VendorName,
                v.VendorCode,
                v.ContactProofFilePath,
                v.CreatedAt,
                v.LinkedConsultantId,
                LcFn = v.LinkedConsultant != null ? v.LinkedConsultant.FirstName : null,
                LcLn = v.LinkedConsultant != null ? v.LinkedConsultant.LastName : null,
                SrFirst = v.SalesRecruiter != null ? v.SalesRecruiter.FirstName : null,
                SrLast = v.SalesRecruiter != null ? v.SalesRecruiter.LastName : null
            })
            .ToListAsync();
        foreach (var v in vendors)
        {
            string? consultantLabel = null;
            string? folder = null;
            if (v.LinkedConsultantId.HasValue && v.LcFn != null)
            {
                consultantLabel = $"{v.LcFn} {v.LcLn}".Trim();
                folder = ConsultantFolderNameHelper.BuildSegment(v.LcFn, v.LcLn ?? "", v.LinkedConsultantId.Value);
            }
            else
            {
                consultantLabel = "Unassigned";
                folder = "_unassigned";
            }
            list.Add(new ManagementFileCatalogItemDto
            {
                Kind = "VendorContactProof",
                Id = v.Id,
                Title = "Vendor contact proof",
                Subtitle = $"{v.VendorName} ({v.VendorCode})",
                ConsultantName = consultantLabel,
                ConsultantStorageFolder = folder,
                VendorName = v.VendorName,
                SalesRecruiterName = v.SrFirst != null ? $"{v.SrFirst} {v.SrLast}" : null,
                FileName = Path.GetFileName(v.ContactProofFilePath!),
                HasFile = true,
                At = v.CreatedAt
            });
        }

        var subs = await _db.Submissions.AsNoTracking()
            .Where(s => s.ProofFilePath != null && s.ProofFilePath != "")
            .Select(s => new
            {
                s.Id,
                s.SubmissionCode,
                s.JobTitle,
                s.ProofFilePath,
                s.SubmissionDate,
                s.ConsultantId,
                Fn = s.Consultant.FirstName,
                Ln = s.Consultant.LastName,
                SrFirst = s.SalesRecruiter.FirstName,
                SrLast = s.SalesRecruiter.LastName,
                s.Vendor.VendorName
            })
            .ToListAsync();
        foreach (var s in subs)
        {
            var consultantLabel = $"{s.Fn} {s.Ln}".Trim();
            var folder = ConsultantFolderNameHelper.BuildSegment(s.Fn, s.Ln, s.ConsultantId);
            list.Add(new ManagementFileCatalogItemDto
            {
                Kind = "SubmissionProof",
                Id = s.Id,
                Title = "Submission proof",
                Subtitle = $"{s.SubmissionCode} — {s.JobTitle}",
                ConsultantName = consultantLabel,
                ConsultantStorageFolder = folder,
                SalesRecruiterName = $"{s.SrFirst} {s.SrLast}",
                VendorName = s.VendorName,
                FileName = Path.GetFileName(s.ProofFilePath!),
                HasFile = true,
                At = s.SubmissionDate
            });
        }

        var ints = await _db.Interviews.AsNoTracking()
            .Where(i => i.InviteProofFilePath != null && i.InviteProofFilePath != "")
            .Select(i => new
            {
                i.Id,
                i.InterviewCode,
                i.InterviewDate,
                i.InviteProofFilePath,
                SubCode = i.Submission.SubmissionCode,
                JobTitle = i.Submission.JobTitle,
                ConsultantId = i.Submission.ConsultantId,
                Fn = i.Submission.Consultant.FirstName,
                Ln = i.Submission.Consultant.LastName,
                SrFirst = i.Submission.SalesRecruiter.FirstName,
                SrLast = i.Submission.SalesRecruiter.LastName
            })
            .ToListAsync();
        foreach (var i in ints)
        {
            var consultantLabel = $"{i.Fn} {i.Ln}".Trim();
            var folder = ConsultantFolderNameHelper.BuildSegment(i.Fn, i.Ln, i.ConsultantId);
            list.Add(new ManagementFileCatalogItemDto
            {
                Kind = "InterviewInviteProof",
                Id = i.Id,
                Title = "Interview invite proof",
                Subtitle = $"{i.InterviewCode} — {i.SubCode} / {i.JobTitle}",
                ConsultantName = consultantLabel,
                ConsultantStorageFolder = folder,
                SalesRecruiterName = $"{i.SrFirst} {i.SrLast}",
                FileName = Path.GetFileName(i.InviteProofFilePath!),
                HasFile = true,
                At = i.InterviewDate
            });
        }

        return list.OrderByDescending(x => x.At).ToList();
    }

    public async Task<(bool Success, string? Error, string? PhysicalPath, string DownloadFileName)> GetFileCatalogDownloadAsync(
        string kind, int id)
    {
        var k = kind.Trim().ToLowerInvariant();
        string? rel = null;
        string? preferredName = null;

        switch (k)
        {
            case "document":
                var d = await _db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (d is null) return (false, "Document not found.", null, string.Empty);
                rel = d.FilePath;
                preferredName = d.FileName;
                break;
            case "vendor":
            case "vendorcontactproof":
                var v = await _db.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (v is null) return (false, "Vendor not found.", null, string.Empty);
                rel = v.ContactProofFilePath;
                preferredName = $"{v.VendorCode}-contact{Path.GetExtension(rel ?? "")}";
                break;
            case "submission":
            case "submissionproof":
                var s = await _db.Submissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (s is null) return (false, "Submission not found.", null, string.Empty);
                rel = s.ProofFilePath;
                preferredName = $"{s.SubmissionCode}-proof{Path.GetExtension(rel ?? "")}";
                break;
            case "interview":
            case "interviewinviteproof":
                var i = await _db.Interviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (i is null) return (false, "Interview not found.", null, string.Empty);
                rel = i.InviteProofFilePath;
                preferredName = $"{i.InterviewCode}-invite{Path.GetExtension(rel ?? "")}";
                break;
            default:
                return (false, "Invalid kind.", null, string.Empty);
        }

        var (ok, err, path, name) = WwwrootFileResolver.TryResolve(_env.ContentRootPath, rel, preferredName);
        if (!ok) return (false, err, null, string.Empty);
        return (true, null, path, name);
    }

    public async Task<(bool Success, string? Error)> ReviewDocumentAsync(string reviewerUserId, int id, ReviewDocumentRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Status) || !AllowedReviewStatuses.Contains(dto.Status.Trim()))
            return (false, "Status must be Approved, Rejected, or Re-check.");

        var status = dto.Status.Trim();
        var d = await _db.Documents.FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return (false, "Document not found.");

        var user = await _users.FindByIdAsync(reviewerUserId);
        if (user is null) return (false, "User not found.");

        var roles = await _users.GetRolesAsync(user);
        var rank = ReviewerRank(roles);
        var primary = PrimaryReviewerAuthority(roles);
        if (primary is null)
            return (false, "You are not allowed to review documents.");

        var now = DateTime.UtcNow;

        if (roles.Contains(nameof(UserRole.Admin)))
        {
            d.Status = status;
            d.ReviewedAt = now;
            d.ReviewedByUserId = reviewerUserId;
            d.AdminReviewLockedAt = now;
            d.LastReviewAuthority = nameof(UserRole.Admin);
            await _db.SaveChangesAsync();
            await _notifications.NotifyDocumentReviewedAsync(d.ConsultantId, d.Id, d.FileName, status);
            return (true, null);
        }

        if (d.AdminReviewLockedAt.HasValue || StoredAuthorityRank(d.LastReviewAuthority) >= StoredAuthorityRank(nameof(UserRole.Admin)))
            return (false, "This document was finalized by an admin and cannot be changed.");

        if (rank < StoredAuthorityRank(d.LastReviewAuthority))
            return (false, "A higher-level review already applies. You cannot override it.");

        d.Status = status;
        d.ReviewedAt = now;
        d.ReviewedByUserId = reviewerUserId;
        d.LastReviewAuthority = primary;
        await _db.SaveChangesAsync();
        await _notifications.NotifyDocumentReviewedAsync(d.ConsultantId, d.Id, d.FileName, status);
        return (true, null);
    }
}
