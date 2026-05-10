using System.Security.Claims;
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

public class ConsultantPortalService : IConsultantPortalService
{
    private const long MaxUploadBytes = 20 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg", ".txt"];

    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _env;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;
    private readonly int _orgId;

    public ConsultantPortalService(
        ApplicationDbContext db,
        IHostEnvironment env,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications,
        ICurrentOrganization tenant)
    {
        _db = db;
        _env = env;
        _userManager = userManager;
        _notifications = notifications;
        _orgId = tenant.OrganizationId;
    }

    private async Task<int?> ResolveConsultantIdAsync(string? userId, bool isAdminOrManagement, int? consultantId)
    {
        if (isAdminOrManagement && consultantId.HasValue)
        {
            var ok = await _db.Consultants.AsNoTracking()
                .AnyAsync(c => c.Id == consultantId.Value && c.OrganizationId == _orgId);
            return ok ? consultantId : null;
        }

        if (userId is null) return null;
        return await _db.Consultants.AsNoTracking()
            .Where(c => c.UserId == userId && c.OrganizationId == _orgId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<ConsultantDashboardDto?> GetDashboardAsync(ClaimsPrincipal user, bool isAdminOrManagement)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, null);
        if (!cid.HasValue) return null;

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var jobsToday = await _db.JobApplications.CountAsync(j =>
            j.ConsultantId == cid && j.AppliedDate >= today && j.AppliedDate < tomorrow);
        var reachToday = await _db.ConsultantVendorReachOuts.CountAsync(r =>
            r.ConsultantId == cid && r.ReachedDate >= today && r.ReachedDate < tomorrow);
        var subsToday = await _db.Submissions.CountAsync(s =>
            s.ConsultantId == cid && s.SubmissionDate >= today && s.SubmissionDate < tomorrow);
        var intToday = await _db.Interviews.CountAsync(i =>
            i.Submission.ConsultantId == cid && i.InterviewDate >= today && i.InterviewDate < tomorrow);
        var respToday = await _db.ConsultantVendorReachOuts.CountAsync(r =>
            r.ConsultantId == cid && r.ReachedDate >= today && r.ReachedDate < tomorrow &&
            r.VendorResponseNotes != null && r.VendorResponseNotes != "");

        return new ConsultantDashboardDto
        {
            JobsAppliedToday = jobsToday,
            VendorsReachedOut = reachToday,
            VendorResponses = respToday,
            Submissions = subsToday,
            InterviewCalls = intToday
        };
    }

    public async Task<(bool Success, string? Error)> UpdateProfileContactAsync(
        ClaimsPrincipal user, UpdateConsultantContactRequestDto dto)
    {
        if (!ValidationHelper.IsValidEmail(dto.Email)) return (false, "A valid email is required.");
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) return (false, "Phone number is required.");

        var userId = UserContextHelper.GetUserId(user);
        if (string.IsNullOrEmpty(userId)) return (false, "User not found.");

        var consultant = await _db.Consultants.FirstOrDefaultAsync(c => c.UserId == userId && c.OrganizationId == _orgId);
        if (consultant is null) return (false, "Consultant not found.");

        var appUser = await _userManager.FindByIdAsync(userId);
        if (appUser is null) return (false, "User not found.");

        var norm = _userManager.NormalizeEmail(dto.Email.Trim());
        if (await _userManager.Users.AnyAsync(u =>
                u.Id != userId && !u.IsDeleted && u.NormalizedEmail == norm && u.OrganizationId == appUser.OrganizationId))
            return (false, "That email is already in use.");

        var email = dto.Email.Trim();
        consultant.Email = email;
        consultant.PhoneNumber = dto.PhoneNumber.Trim();
        consultant.UpdatedAt = DateTime.UtcNow;

        appUser.Email = email;
        appUser.UserName = email;
        appUser.NormalizedEmail = norm;
        appUser.NormalizedUserName = _userManager.NormalizeName(email);
        appUser.PhoneNumber = dto.PhoneNumber.Trim();
        appUser.UpdatedAt = DateTime.UtcNow;

        var ur = await _userManager.UpdateAsync(appUser);
        if (!ur.Succeeded)
            return (false, string.Join(" ", ur.Errors.Select(e => e.Description)));

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<ConsultantProfileDto?> GetProfileAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId)
    {
        var userId = UserContextHelper.GetUserId(user);
        var query = _db.Consultants.AsNoTracking().Where(c => c.OrganizationId == _orgId);
        if (isAdminOrManagement && consultantId.HasValue)
            query = query.Where(c => c.Id == consultantId.Value);
        else if (userId != null)
            query = query.Where(c => c.UserId == userId);
        else return null;

        return await query.Select(c => new ConsultantProfileDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            VisaStatus = c.VisaStatus,
            Technology = c.Technology,
            SkillsNotes = c.SkillsNotes,
            ExperienceYears = c.ExperienceYears,
            CurrentLocation = c.CurrentLocation,
            Status = c.Status
        }).FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string? Error)> CreateDailyActivityAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, CreateDailyActivityRequestDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return (false, "Consultant not found.");

        var date = dto.ActivityDate.Date;
        if (await _db.DailyActivities.AnyAsync(d => d.ConsultantId == cid && d.ActivityDate.Date == date))
            return (false, "Daily activity already exists for this date.");

        int jobs, reach, resp, subs, ints;
        if (!isAdminOrManagement)
        {
            (jobs, reach, resp, subs, ints) = await GetDerivedDailyCountsAsync(cid.Value, date);
        }
        else
        {
            jobs = dto.JobsAppliedCount;
            reach = dto.VendorReachedOutCount;
            resp = dto.VendorResponseCount;
            subs = dto.SubmissionsCount;
            ints = dto.InterviewCallsCount;
        }

        _db.DailyActivities.Add(new DailyActivity
        {
            ConsultantId = cid.Value,
            ActivityDate = date,
            JobsAppliedCount = jobs,
            VendorReachedOutCount = reach,
            VendorResponseCount = resp,
            SubmissionsCount = subs,
            InterviewCallsCount = ints,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<IReadOnlyList<DailyActivityDto>> GetDailyActivitiesAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return Array.Empty<DailyActivityDto>();

        return await _db.DailyActivities.AsNoTracking()
            .Where(d => d.ConsultantId == cid)
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

    public async Task<(bool Success, string? Error)> UpdateDailyActivityAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int activityId, CreateDailyActivityRequestDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, null);
        if (!cid.HasValue) return (false, "Consultant not found.");

        var d = await _db.DailyActivities.FirstOrDefaultAsync(x => x.Id == activityId && x.ConsultantId == cid);
        if (d is null) return (false, "Activity not found.");

        var newDate = dto.ActivityDate.Date;
        if (await _db.DailyActivities.AnyAsync(x => x.ConsultantId == cid && x.ActivityDate.Date == newDate && x.Id != activityId))
            return (false, "Daily activity already exists for this date.");

        d.ActivityDate = newDate;
        if (!isAdminOrManagement)
        {
            var (jobs, reach, resp, subs, ints) = await GetDerivedDailyCountsAsync(cid.Value, newDate);
            d.JobsAppliedCount = jobs;
            d.VendorReachedOutCount = reach;
            d.VendorResponseCount = resp;
            d.SubmissionsCount = subs;
            d.InterviewCallsCount = ints;
        }
        else
        {
            d.JobsAppliedCount = dto.JobsAppliedCount;
            d.VendorReachedOutCount = dto.VendorReachedOutCount;
            d.VendorResponseCount = dto.VendorResponseCount;
            d.SubmissionsCount = dto.SubmissionsCount;
            d.InterviewCallsCount = dto.InterviewCallsCount;
        }

        d.Notes = dto.Notes;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<DailyActivitySuggestionsDto?> GetDailyActivitySuggestionsAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, DateTime activityDate)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return null;
        var date = activityDate.Date;
        var (jobs, reach, resp, subs, ints) = await GetDerivedDailyCountsAsync(cid.Value, date);
        return new DailyActivitySuggestionsDto
        {
            JobsAppliedCount = jobs,
            VendorReachedOutCount = reach,
            VendorResponseCount = resp,
            SubmissionsCount = subs,
            InterviewCallsCount = ints
        };
    }

    private async Task<(int Jobs, int Reach, int VendorResponses, int Subs, int Ints)> GetDerivedDailyCountsAsync(int consultantId, DateTime date)
    {
        var day = date.Date;
        var next = day.AddDays(1);
        var jobs = await _db.JobApplications.CountAsync(j =>
            j.ConsultantId == consultantId && j.AppliedDate >= day && j.AppliedDate < next);
        var reach = await _db.ConsultantVendorReachOuts.CountAsync(r =>
            r.ConsultantId == consultantId && r.ReachedDate >= day && r.ReachedDate < next);
        var resp = await _db.ConsultantVendorReachOuts.CountAsync(r =>
            r.ConsultantId == consultantId && r.ReachedDate >= day && r.ReachedDate < next &&
            r.VendorResponseNotes != null && r.VendorResponseNotes != "");
        var subs = await _db.Submissions.CountAsync(s =>
            s.ConsultantId == consultantId && s.SubmissionDate >= day && s.SubmissionDate < next);
        var ints = await _db.Interviews.CountAsync(i =>
            i.Submission.ConsultantId == consultantId && i.InterviewDate >= day && i.InterviewDate < next);
        return (jobs, reach, resp, subs, ints);
    }

    public async Task<IReadOnlyList<ConsultantVendorReachOutDto>> GetVendorReachOutsAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return Array.Empty<ConsultantVendorReachOutDto>();

        return await _db.ConsultantVendorReachOuts.AsNoTracking()
            .Where(r => r.ConsultantId == cid)
            .OrderByDescending(r => r.ReachedDate)
            .Select(r => new ConsultantVendorReachOutDto
            {
                Id = r.Id,
                ReachedDate = r.ReachedDate,
                VendorName = r.VendorName,
                ContactPerson = r.ContactPerson,
                ContactEmail = r.ContactEmail,
                VendorResponseNotes = r.VendorResponseNotes,
                Notes = r.Notes
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CreateVendorReachOutAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, CreateConsultantVendorReachOutDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return (false, "Consultant not found.");
        if (string.IsNullOrWhiteSpace(dto.VendorName)) return (false, "Vendor name is required.");

        _db.ConsultantVendorReachOuts.Add(new ConsultantVendorReachOut
        {
            ConsultantId = cid.Value,
            ReachedDate = dto.ReachedDate.Date,
            VendorName = dto.VendorName.Trim(),
            ContactPerson = string.IsNullOrWhiteSpace(dto.ContactPerson) ? null : dto.ContactPerson.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail) ? null : dto.ContactEmail.Trim(),
            VendorResponseNotes = string.IsNullOrWhiteSpace(dto.VendorResponseNotes) ? null : dto.VendorResponseNotes.Trim(),
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        await RefreshDailyActivityDerivedCountsForDateAsync(cid.Value, dto.ReachedDate.Date);
        return (true, null);
    }

    public async Task<IReadOnlyList<ConsultantInterviewDto>> GetConsultantInterviewsAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return Array.Empty<ConsultantInterviewDto>();

        return await _db.Interviews.AsNoTracking()
            .Where(i => i.Submission.ConsultantId == cid)
            .OrderByDescending(i => i.InterviewDate)
            .Select(i => new ConsultantInterviewDto
            {
                Id = i.Id,
                InterviewCode = i.InterviewCode,
                SubmissionCode = i.Submission.SubmissionCode,
                JobTitle = i.Submission.JobTitle,
                InterviewDate = i.InterviewDate,
                InterviewEndDate = i.InterviewEndDate,
                InterviewMode = i.InterviewMode,
                Round = i.Round,
                Status = i.Status,
                Feedback = i.Feedback,
                Notes = i.Notes,
                HasInviteProof = i.InviteProofFilePath != null && i.InviteProofFilePath != ""
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CreateJobApplicationAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, CreateJobApplicationRequestDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return (false, "Consultant not found.");

        _db.JobApplications.Add(new JobApplication
        {
            ConsultantId = cid.Value,
            JobTitle = dto.JobTitle,
            CompanyName = dto.CompanyName,
            ClientName = dto.ClientName,
            Source = dto.Source,
            JobUrl = dto.JobUrl,
            AppliedDate = dto.AppliedDate,
            Status = dto.Status,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<IReadOnlyList<JobApplicationDto>> GetJobApplicationsAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return Array.Empty<JobApplicationDto>();

        return await _db.JobApplications.AsNoTracking()
            .Where(j => j.ConsultantId == cid)
            .OrderByDescending(j => j.AppliedDate)
            .Select(j => new JobApplicationDto
            {
                Id = j.Id,
                JobTitle = j.JobTitle,
                CompanyName = j.CompanyName,
                ClientName = j.ClientName,
                Source = j.Source,
                JobUrl = j.JobUrl,
                AppliedDate = j.AppliedDate,
                Status = j.Status,
                Notes = j.Notes
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateJobApplicationAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int id, CreateJobApplicationRequestDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, null);
        if (!cid.HasValue) return (false, "Consultant not found.");

        var j = await _db.JobApplications.FirstOrDefaultAsync(x => x.Id == id && x.ConsultantId == cid);
        if (j is null) return (false, "Job application not found.");

        j.JobTitle = dto.JobTitle;
        j.CompanyName = dto.CompanyName;
        j.ClientName = dto.ClientName;
        j.Source = dto.Source;
        j.JobUrl = dto.JobUrl;
        j.AppliedDate = dto.AppliedDate;
        j.Status = dto.Status;
        j.Notes = dto.Notes;
        j.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<IReadOnlyList<ConsultantSubmissionDto>> GetSubmissionsAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return Array.Empty<ConsultantSubmissionDto>();

        return await _db.Submissions.AsNoTracking()
            .Where(s => s.ConsultantId == cid)
            .OrderByDescending(s => s.SubmissionDate)
            .Select(s => new ConsultantSubmissionDto
            {
                Id = s.Id,
                SubmissionCode = s.SubmissionCode,
                JobTitle = s.JobTitle,
                ClientName = s.ClientName,
                VendorName = s.Vendor.VendorName,
                SalesRecruiterName = s.SalesRecruiter.FirstName + " " + s.SalesRecruiter.LastName,
                SubmissionDate = s.SubmissionDate,
                Status = s.Status,
                Notes = s.Notes,
                ConsultantCommunication = s.ConsultantCommunication,
                HasProof = s.ProofFilePath != null && s.ProofFilePath != ""
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return Array.Empty<DocumentDto>();

        var rows = await _db.Documents.AsNoTracking()
            .Where(d => d.ConsultantId == cid.Value)
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

    public async Task<(bool Success, string? Error, DocumentDto? Doc)> UploadDocumentAsync(
        ClaimsPrincipal user,
        bool isAdminOrManagement,
        int? consultantId,
        string documentType,
        Stream fileStream,
        string originalFileName,
        long fileLength)
    {
        if (string.IsNullOrWhiteSpace(documentType)) return (false, "Document type is required.", null);
        if (fileLength <= 0 || fileLength > MaxUploadBytes) return (false, "File is empty or too large (max 20 MB).", null);

        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return (false, "Consultant not found.", null);

        var consultant = await _db.Consultants.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cid.Value && c.OrganizationId == _orgId);
        if (consultant is null) return (false, "Consultant not found.", null);

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return (false, "File type not allowed. Use PDF, Word, images, or TXT.", null);

        var safeName = Path.GetFileName(originalFileName);
        var unique = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{safeName}";
        var dirSegment = ConsultantFolderNameHelper.BuildSegment(consultant.FirstName, consultant.LastName, consultant.Id);
        var relativeDir = Path.Combine("uploads", dirSegment, "documents");
        var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(wwwroot, relativeDir);
        Directory.CreateDirectory(dir);
        var relativePath = Path.Combine(relativeDir, unique).Replace('\\', '/');
        var physicalPath = Path.Combine(wwwroot, relativePath);

        await using (var output = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(output);
        }

        var doc = new Document
        {
            ConsultantId = cid.Value,
            DocumentType = documentType.Trim(),
            FileName = safeName,
            FilePath = relativePath,
            Status = "Pending",
            UploadedAt = DateTime.UtcNow
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        if (isAdminOrManagement)
            await _notifications.NotifyManagementUploadedDocumentAsync(doc.ConsultantId, doc.Id, doc.DocumentType, doc.FileName);
        else
            await _notifications.NotifyConsultantUploadedDocumentPendingReviewAsync(doc.ConsultantId, doc.Id, doc.DocumentType, doc.FileName);

        var dto = new DocumentDto
        {
            Id = doc.Id,
            ConsultantId = doc.ConsultantId,
            ConsultantName = await _db.Consultants.AsNoTracking()
                .Where(c => c.Id == doc.ConsultantId)
                .Select(c => c.FirstName + " " + c.LastName)
                .FirstAsync(),
            StorageFolder = ConsultantFolderNameHelper.BuildSegment(consultant.FirstName, consultant.LastName, consultant.Id),
            DocumentType = doc.DocumentType,
            FileName = doc.FileName,
            UploadedAt = doc.UploadedAt,
            Status = doc.Status,
            LockedAfterAdminDecision = false,
            LastReviewAuthority = null
        };

        return (true, null, dto);
    }

    public async Task<(bool Success, string? Error, string? PhysicalPath, string DownloadFileName)> GetDocumentDownloadAsync(
        ClaimsPrincipal user,
        bool isAdminOrManagement,
        int documentId)
    {
        var doc = await _db.Documents.Include(d => d.Consultant).FirstOrDefaultAsync(d => d.Id == documentId);
        if (doc is null) return (false, "Document not found.", null, string.Empty);
        if (doc.Consultant.OrganizationId != _orgId) return (false, "Forbidden.", null, string.Empty);

        var elevated = UserContextHelper.IsInAnyRole(user,
            UserRole.Admin.ToString(),
            UserRole.Management.ToString());

        if (!elevated)
        {
            var userId = UserContextHelper.GetUserId(user);
            var cid = await ResolveConsultantIdAsync(userId, false, null);
            if (!cid.HasValue || doc.ConsultantId != cid.Value)
                return (false, "Forbidden.", null, string.Empty);
        }

        var (ok, err, full, name) = WwwrootFileResolver.TryResolve(_env.ContentRootPath, doc.FilePath, doc.FileName);
        if (!ok) return (false, err, null, string.Empty);
        return (true, null, full, name);
    }

    public async Task<(bool Success, string? Error, string? PhysicalPath, string DownloadFileName)> GetConsultantProofDownloadAsync(
        ClaimsPrincipal user, string kind, int id)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, false, null);
        if (!cid.HasValue) return (false, "Consultant not found.", null, string.Empty);

        var k = kind.Trim().ToLowerInvariant();
        string? rel = null;
        string? preferredName = null;

        if (k == "submission")
        {
            var s = await _db.Submissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return (false, "Submission not found.", null, string.Empty);
            if (s.ConsultantId != cid.Value) return (false, "Forbidden.", null, string.Empty);
            rel = s.ProofFilePath;
            preferredName = $"{s.SubmissionCode}-proof{Path.GetExtension(rel ?? "")}";
        }
        else if (k == "interview")
        {
            var i = await _db.Interviews.AsNoTracking().Include(x => x.Submission).FirstOrDefaultAsync(x => x.Id == id);
            if (i is null) return (false, "Interview not found.", null, string.Empty);
            if (i.Submission.ConsultantId != cid.Value) return (false, "Forbidden.", null, string.Empty);
            rel = i.InviteProofFilePath;
            preferredName = $"{i.InterviewCode}-invite{Path.GetExtension(rel ?? "")}";
        }
        else
            return (false, "Invalid kind. Use submission or interview.", null, string.Empty);

        var (ok, err, path, name) = WwwrootFileResolver.TryResolve(_env.ContentRootPath, rel, preferredName);
        if (!ok) return (false, err, null, string.Empty);
        return (true, null, path, name);
    }

    public async Task<(bool Success, string? Error)> PatchDailyActivityNotesAsync(
        ClaimsPrincipal user, int activityId, PatchDailyActivityNotesDto dto)
    {
        if (UserContextHelper.IsInAnyRole(user, UserRole.Admin.ToString(), UserRole.Management.ToString()))
            return (false, "Use the full daily activity update for admin or management users.");
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, false, null);
        if (!cid.HasValue) return (false, "Consultant not found.");
        var d = await _db.DailyActivities.FirstOrDefaultAsync(x => x.Id == activityId && x.ConsultantId == cid);
        if (d is null) return (false, "Activity not found.");
        d.Notes = dto.Notes;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateVendorReachOutAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, int reachOutId, UpdateConsultantVendorReachOutDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, isAdminOrManagement, consultantId);
        if (!cid.HasValue) return (false, "Consultant not found.");
        var r = await _db.ConsultantVendorReachOuts.FirstOrDefaultAsync(x => x.Id == reachOutId && x.ConsultantId == cid);
        if (r is null) return (false, "Reach-out not found.");
        if (string.IsNullOrWhiteSpace(dto.VendorName)) return (false, "Vendor name is required.");
        var oldDate = r.ReachedDate.Date;
        r.ReachedDate = dto.ReachedDate.Date;
        r.VendorName = dto.VendorName.Trim();
        r.ContactPerson = string.IsNullOrWhiteSpace(dto.ContactPerson) ? null : dto.ContactPerson.Trim();
        r.ContactEmail = string.IsNullOrWhiteSpace(dto.ContactEmail) ? null : dto.ContactEmail.Trim();
        r.VendorResponseNotes = string.IsNullOrWhiteSpace(dto.VendorResponseNotes) ? null : dto.VendorResponseNotes.Trim();
        r.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        await RefreshDailyActivityDerivedCountsForDateAsync(cid.Value, oldDate);
        if (oldDate != r.ReachedDate.Date)
            await RefreshDailyActivityDerivedCountsForDateAsync(cid.Value, r.ReachedDate.Date);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateSubmissionConsultantCommunicationAsync(
        ClaimsPrincipal user, int submissionId, UpdateConsultantSubmissionCommunicationDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, false, null);
        if (!cid.HasValue) return (false, "Consultant not found.");
        var s = await _db.Submissions.FirstOrDefaultAsync(x => x.Id == submissionId && x.ConsultantId == cid);
        if (s is null) return (false, "Submission not found.");
        s.ConsultantCommunication = dto.ConsultantCommunication;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateConsultantInterviewAsync(
        ClaimsPrincipal user, int interviewId, UpdateConsultantInterviewDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var cid = await ResolveConsultantIdAsync(userId, false, null);
        if (!cid.HasValue) return (false, "Consultant not found.");
        var i = await _db.Interviews.Include(x => x.Submission).FirstOrDefaultAsync(x => x.Id == interviewId);
        if (i is null) return (false, "Interview not found.");
        if (i.Submission.ConsultantId != cid.Value) return (false, "Forbidden.");
        if (dto.InterviewEndDate.HasValue && dto.InterviewEndDate.Value < dto.InterviewDate)
            return (false, "Interview end time must be on or after the start time.");
        i.InterviewDate = dto.InterviewDate;
        i.InterviewEndDate = dto.InterviewEndDate;
        i.InterviewMode = dto.InterviewMode;
        i.Round = dto.Round;
        i.Status = dto.Status;
        i.Feedback = dto.Feedback;
        i.Notes = dto.Notes;
        i.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task RefreshDailyActivityDerivedCountsForDateAsync(int consultantId, DateTime activityDate)
    {
        var day = activityDate.Date;
        var d = await _db.DailyActivities.FirstOrDefaultAsync(x =>
            x.ConsultantId == consultantId && x.ActivityDate.Date == day);
        if (d is null) return;
        var (jobs, reach, resp, subs, ints) = await GetDerivedDailyCountsAsync(consultantId, day);
        d.JobsAppliedCount = jobs;
        d.VendorReachedOutCount = reach;
        d.VendorResponseCount = resp;
        d.SubmissionsCount = subs;
        d.InterviewCallsCount = ints;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
