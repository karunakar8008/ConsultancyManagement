using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ConsultancyManagement.Infrastructure.Data;

namespace ConsultancyManagement.Infrastructure.Services;

public class ManagementPortalService : IManagementPortalService
{
    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _env;

    public ManagementPortalService(ApplicationDbContext db, IHostEnvironment env)
    {
        _db = db;
        _env = env;
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
                Status = s.Status
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
        return await _db.Documents.AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                ConsultantId = d.ConsultantId,
                ConsultantName = d.Consultant.FirstName + " " + d.Consultant.LastName,
                DocumentType = d.DocumentType,
                FileName = d.FileName,
                UploadedAt = d.UploadedAt,
                Status = d.Status
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<ManagementFileCatalogItemDto>> GetFileCatalogAsync()
    {
        var list = new List<ManagementFileCatalogItemDto>();

        var docs = await _db.Documents.AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id,
                d.DocumentType,
                d.FileName,
                d.FilePath,
                d.UploadedAt,
                Consultant = d.Consultant.FirstName + " " + d.Consultant.LastName
            })
            .ToListAsync();
        foreach (var d in docs)
        {
            list.Add(new ManagementFileCatalogItemDto
            {
                Kind = "Document",
                Id = d.Id,
                Title = d.DocumentType,
                Subtitle = "Consultant document",
                ConsultantName = d.Consultant,
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
                SrFirst = v.SalesRecruiter != null ? v.SalesRecruiter.FirstName : null,
                SrLast = v.SalesRecruiter != null ? v.SalesRecruiter.LastName : null
            })
            .ToListAsync();
        foreach (var v in vendors)
        {
            list.Add(new ManagementFileCatalogItemDto
            {
                Kind = "VendorContactProof",
                Id = v.Id,
                Title = "Vendor contact proof",
                Subtitle = $"{v.VendorName} ({v.VendorCode})",
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
                Consultant = s.Consultant.FirstName + " " + s.Consultant.LastName,
                SrFirst = s.SalesRecruiter.FirstName,
                SrLast = s.SalesRecruiter.LastName,
                s.Vendor.VendorName
            })
            .ToListAsync();
        foreach (var s in subs)
        {
            list.Add(new ManagementFileCatalogItemDto
            {
                Kind = "SubmissionProof",
                Id = s.Id,
                Title = "Submission proof",
                Subtitle = $"{s.SubmissionCode} — {s.JobTitle}",
                ConsultantName = s.Consultant,
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
                Consultant = i.Submission.Consultant.FirstName + " " + i.Submission.Consultant.LastName,
                SrFirst = i.Submission.SalesRecruiter.FirstName,
                SrLast = i.Submission.SalesRecruiter.LastName
            })
            .ToListAsync();
        foreach (var i in ints)
        {
            list.Add(new ManagementFileCatalogItemDto
            {
                Kind = "InterviewInviteProof",
                Id = i.Id,
                Title = "Interview invite proof",
                Subtitle = $"{i.InterviewCode} — {i.SubCode} / {i.JobTitle}",
                ConsultantName = i.Consultant,
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
        var d = await _db.Documents.FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return (false, "Document not found.");
        d.Status = dto.Status;
        d.ReviewedAt = DateTime.UtcNow;
        d.ReviewedByUserId = reviewerUserId;
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
