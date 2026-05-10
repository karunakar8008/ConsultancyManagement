using System.Security.Claims;
using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ConsultancyManagement.Infrastructure.Data;

namespace ConsultancyManagement.Infrastructure.Services;

public class SalesPortalService : ISalesPortalService
{
    private readonly ApplicationDbContext _db;
    private readonly IHostEnvironment _env;
    private readonly int _orgId;

    public SalesPortalService(ApplicationDbContext db, IHostEnvironment env, ICurrentOrganization tenant)
    {
        _db = db;
        _env = env;
        _orgId = tenant.OrganizationId;
    }

    private async Task<int?> GetSalesRecruiterIdAsync(string? userId)
    {
        if (userId is null) return null;
        return await _db.SalesRecruiters.AsNoTracking()
            .Where(s => s.UserId == userId && s.OrganizationId == _orgId)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<bool> IsAssignedAsync(int salesId, int consultantId) =>
        await _db.ConsultantSalesAssignments.AnyAsync(a =>
            a.SalesRecruiterId == salesId && a.ConsultantId == consultantId && a.IsActive);

    private static string NewVendorCode() => "VEN-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    private static string NewSubmissionCode() => "SUB-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    private static string NewInterviewCode() => "INT-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    public async Task<SalesDashboardDto?> GetDashboardAsync(ClaimsPrincipal user, bool isElevated)
    {
        var userId = UserContextHelper.GetUserId(user);
        int? salesId = null;
        if (!isElevated)
        {
            salesId = await GetSalesRecruiterIdAsync(userId);
            if (!salesId.HasValue) return null;
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        if (salesId.HasValue)
        {
            var assigned = await _db.ConsultantSalesAssignments.CountAsync(a => a.SalesRecruiterId == salesId && a.IsActive);
            var vendorsToday = await _db.Vendors.CountAsync(v =>
                v.SalesRecruiterId == salesId && v.CreatedAt >= today && v.CreatedAt < tomorrow);
            var subsToday = await _db.Submissions.CountAsync(s =>
                s.SalesRecruiterId == salesId && s.SubmissionDate >= today && s.SubmissionDate < tomorrow);
            var interviews = await _db.Interviews.CountAsync(i =>
                i.Submission.SalesRecruiterId == salesId && i.InterviewDate >= today && i.InterviewDate < tomorrow);

            return new SalesDashboardDto
            {
                AssignedConsultants = assigned,
                VendorsContactedToday = vendorsToday,
                SubmissionsToday = subsToday,
                InterviewsScheduled = interviews
            };
        }

        var vendorsAllToday = await _db.Vendors.CountAsync(v =>
            v.OrganizationId == _orgId && v.CreatedAt >= today && v.CreatedAt < tomorrow);
        return new SalesDashboardDto
        {
            AssignedConsultants = await _db.ConsultantSalesAssignments.CountAsync(a =>
                a.IsActive && a.Consultant.OrganizationId == _orgId),
            VendorsContactedToday = vendorsAllToday,
            SubmissionsToday = await _db.Submissions.CountAsync(s =>
                s.SubmissionDate >= today && s.SubmissionDate < tomorrow && s.Consultant.OrganizationId == _orgId),
            InterviewsScheduled = await _db.Interviews.CountAsync(i =>
                i.InterviewDate >= today && i.InterviewDate < tomorrow &&
                i.Submission.Consultant.OrganizationId == _orgId)
        };
    }

    public async Task<IReadOnlyList<AssignedConsultantDto>> GetAssignedConsultantsAsync(ClaimsPrincipal user, bool isElevated)
    {
        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        if (!isElevated && !salesId.HasValue) return Array.Empty<AssignedConsultantDto>();

        var q = from a in _db.ConsultantSalesAssignments.AsNoTracking()
            join c in _db.Consultants.AsNoTracking() on a.ConsultantId equals c.Id
            where c.OrganizationId == _orgId && a.IsActive && (isElevated || a.SalesRecruiterId == salesId)
            select new AssignedConsultantDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Technology = c.Technology,
                VisaStatus = c.VisaStatus,
                CurrentLocation = c.CurrentLocation,
                Status = c.Status
            };
        return await q.Distinct().OrderBy(x => x.LastName).ToListAsync();
    }

    public async Task<AssignedConsultantDto?> GetAssignedConsultantAsync(ClaimsPrincipal user, bool isElevated, int consultantId)
    {
        var list = await GetAssignedConsultantsAsync(user, isElevated);
        return list.FirstOrDefault(c => c.Id == consultantId);
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateVendorAsync(
        ClaimsPrincipal user, bool isElevated, CreateVendorRequestDto dto, string? contactProofRelativePath)
    {
        if (string.IsNullOrWhiteSpace(dto.VendorName)) return (false, "Vendor name is required.", null);
        if (string.IsNullOrWhiteSpace(dto.Email) || !ValidationHelper.IsValidEmail(dto.Email))
            return (false, "A valid email is required.", null);
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            return (false, "Phone number is required.", null);

        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        if (!isElevated && !salesId.HasValue) return (false, "Sales recruiter not found.", null);

        int? ownerSalesId = isElevated ? null : salesId;
        int vendorOrgId = _orgId;
        if (ownerSalesId.HasValue)
        {
            vendorOrgId = await _db.SalesRecruiters.AsNoTracking()
                .Where(s => s.Id == ownerSalesId.Value)
                .Select(s => s.OrganizationId)
                .FirstAsync();
        }

        var code = NewVendorCode();
        while (await _db.Vendors.AnyAsync(v => v.OrganizationId == vendorOrgId && v.VendorCode == code))
            code = NewVendorCode();

        if (dto.LinkedConsultantId.HasValue &&
            !await _db.Consultants.AnyAsync(c => c.Id == dto.LinkedConsultantId.Value && c.OrganizationId == _orgId))
            return (false, "Consultant not found.", null);
        if (!isElevated && salesId.HasValue && dto.LinkedConsultantId.HasValue
            && !await IsAssignedAsync(salesId.Value, dto.LinkedConsultantId.Value))
            return (false, "Select a consultant assigned to you for contact proof storage.", null);

        var v = new Vendor
        {
            OrganizationId = vendorOrgId,
            VendorCode = code,
            SalesRecruiterId = ownerSalesId,
            VendorName = dto.VendorName.Trim(),
            ContactPerson = dto.ContactPerson,
            Email = dto.Email.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            CompanyName = dto.CompanyName,
            LinkedInUrl = dto.LinkedInUrl,
            Notes = dto.Notes,
            LinkedConsultantId = dto.LinkedConsultantId,
            ContactProofFilePath = string.IsNullOrWhiteSpace(contactProofRelativePath) ? null : contactProofRelativePath,
            CreatedAt = DateTime.UtcNow
        };
        _db.Vendors.Add(v);
        await _db.SaveChangesAsync();
        return (true, null, v.Id);
    }

    public async Task<IReadOnlyList<VendorDto>> GetVendorsAsync(ClaimsPrincipal user, bool isElevated)
    {
        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);

        var q = _db.Vendors.AsNoTracking().AsQueryable().Where(v => v.OrganizationId == _orgId);
        if (!isElevated && salesId.HasValue)
            q = q.Where(v => v.SalesRecruiterId == salesId);
        else if (!isElevated)
            return Array.Empty<VendorDto>();

        return await q
            .OrderBy(v => v.VendorName)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                VendorCode = v.VendorCode,
                SalesRecruiterId = v.SalesRecruiterId,
                VendorName = v.VendorName,
                ContactPerson = v.ContactPerson,
                Email = v.Email,
                PhoneNumber = v.PhoneNumber,
                CompanyName = v.CompanyName,
                LinkedInUrl = v.LinkedInUrl,
                Notes = v.Notes,
                HasContactProof = v.ContactProofFilePath != null && v.ContactProofFilePath != ""
            }).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> UpdateVendorAsync(
        ClaimsPrincipal user, bool isElevated, int id, CreateVendorRequestDto dto, string? contactProofRelativePath)
    {
        if (string.IsNullOrWhiteSpace(dto.VendorName)) return (false, "Vendor name is required.");
        if (string.IsNullOrWhiteSpace(dto.Email) || !ValidationHelper.IsValidEmail(dto.Email))
            return (false, "A valid email is required.");
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            return (false, "Phone number is required.");

        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        var v = await _db.Vendors.FirstOrDefaultAsync(x => x.Id == id);
        if (v is null) return (false, "Vendor not found.");
        if (v.OrganizationId != _orgId) return (false, "Forbidden.");
        if (!isElevated && (!salesId.HasValue || v.SalesRecruiterId != salesId)) return (false, "Forbidden.");

        v.VendorName = dto.VendorName.Trim();
        v.ContactPerson = dto.ContactPerson;
        v.Email = dto.Email.Trim();
        v.PhoneNumber = dto.PhoneNumber.Trim();
        v.CompanyName = dto.CompanyName;
        v.LinkedInUrl = dto.LinkedInUrl;
        v.Notes = dto.Notes;
        if (dto.LinkedConsultantId.HasValue)
        {
            if (!await _db.Consultants.AnyAsync(c => c.Id == dto.LinkedConsultantId.Value && c.OrganizationId == _orgId))
                return (false, "Consultant not found.");
            if (!isElevated && salesId.HasValue
                && !await IsAssignedAsync(salesId.Value, dto.LinkedConsultantId.Value))
                return (false, "Select a consultant assigned to you for contact proof storage.");
            v.LinkedConsultantId = dto.LinkedConsultantId;
        }
        if (!string.IsNullOrWhiteSpace(contactProofRelativePath))
            v.ContactProofFilePath = contactProofRelativePath;
        v.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateSubmissionAsync(
        ClaimsPrincipal user, bool isElevated, CreateSubmissionRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ProofFilePath))
            return (false, "Submission proof file is required.", null);

        if (!await _db.Consultants.AnyAsync(c => c.Id == dto.ConsultantId && c.OrganizationId == _orgId))
            return (false, "Consultant is required.", null);
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == dto.VendorId && v.OrganizationId == _orgId);
        if (vendor is null) return (false, "Vendor is required.", null);

        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);

        int salesRecruiterId;
        if (isElevated)
        {
            var assignment = await _db.ConsultantSalesAssignments.AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.ConsultantId == dto.ConsultantId && a.IsActive &&
                    a.Consultant.OrganizationId == _orgId);
            if (assignment is null)
                return (false, "Submission requires an active consultant–sales assignment.", null);
            salesRecruiterId = assignment.SalesRecruiterId;
        }
        else
        {
            if (!salesId.HasValue) return (false, "Sales recruiter not found.", null);
            if (!await IsAssignedAsync(salesId.Value, dto.ConsultantId))
                return (false, "Sales recruiter can only submit assigned consultants.", null);
            if (vendor.SalesRecruiterId != salesId)
                return (false, "Select a vendor from your vendor list.", null);
            salesRecruiterId = salesId.Value;
        }

        var code = NewSubmissionCode();
        while (await _db.Submissions.AnyAsync(s => s.SubmissionCode == code))
            code = NewSubmissionCode();

        var sub = new Submission
        {
            SubmissionCode = code,
            ConsultantId = dto.ConsultantId,
            SalesRecruiterId = salesRecruiterId,
            VendorId = dto.VendorId,
            JobTitle = dto.JobTitle,
            ClientName = dto.ClientName,
            SubmissionDate = dto.SubmissionDate,
            Status = dto.Status,
            Rate = dto.Rate,
            Notes = dto.Notes,
            ProofFilePath = dto.ProofFilePath,
            CreatedAt = DateTime.UtcNow
        };
        _db.Submissions.Add(sub);
        await _db.SaveChangesAsync();
        return (true, null, sub.Id);
    }

    public async Task<IReadOnlyList<SalesSubmissionDto>> GetSubmissionsAsync(ClaimsPrincipal user, bool isElevated)
    {
        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        if (!isElevated && !salesId.HasValue) return Array.Empty<SalesSubmissionDto>();

        return await _db.Submissions.AsNoTracking()
            .Where(s => s.Consultant.OrganizationId == _orgId && (isElevated || s.SalesRecruiterId == salesId))
            .OrderByDescending(s => s.SubmissionDate)
            .Select(s => new SalesSubmissionDto
            {
                Id = s.Id,
                SubmissionCode = s.SubmissionCode,
                ConsultantId = s.ConsultantId,
                ConsultantName = s.Consultant.FirstName + " " + s.Consultant.LastName,
                VendorId = s.VendorId,
                VendorName = s.Vendor.VendorName,
                JobTitle = s.JobTitle,
                ClientName = s.ClientName,
                SubmissionDate = s.SubmissionDate,
                Status = s.Status,
                Rate = s.Rate,
                Notes = s.Notes,
                HasProof = s.ProofFilePath != null && s.ProofFilePath != ""
            }).ToListAsync();
    }

    public async Task<IReadOnlyList<SalesSubmissionOptionDto>> GetSubmissionOptionsAsync(ClaimsPrincipal user, bool isElevated)
    {
        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        if (!isElevated && !salesId.HasValue) return Array.Empty<SalesSubmissionOptionDto>();

        return await _db.Submissions.AsNoTracking()
            .Where(s => s.Consultant.OrganizationId == _orgId && (isElevated || s.SalesRecruiterId == salesId))
            .OrderByDescending(s => s.SubmissionDate)
            .Select(s => new SalesSubmissionOptionDto
            {
                Id = s.Id,
                SubmissionCode = s.SubmissionCode,
                JobTitle = s.JobTitle,
                ConsultantName = s.Consultant.FirstName + " " + s.Consultant.LastName
            }).ToListAsync();
    }

    public async Task<SalesSubmissionDto?> GetSubmissionByIdAsync(ClaimsPrincipal user, bool isElevated, int id)
    {
        var list = await GetSubmissionsAsync(user, isElevated);
        return list.FirstOrDefault(s => s.Id == id);
    }

    public async Task<(bool Success, string? Error)> UpdateSubmissionAsync(
        ClaimsPrincipal user, bool isElevated, int id, CreateSubmissionRequestDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        var s = await _db.Submissions.Include(x => x.Consultant).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return (false, "Submission not found.");
        if (s.Consultant.OrganizationId != _orgId) return (false, "Forbidden.");
        if (!isElevated && s.SalesRecruiterId != salesId) return (false, "Forbidden.");

        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == dto.VendorId && v.OrganizationId == _orgId);
        if (vendor is null) return (false, "Vendor is required.");

        if (!isElevated && (!salesId.HasValue || vendor.SalesRecruiterId != salesId))
            return (false, "Select a vendor from your vendor list.");

        if (!isElevated && !await IsAssignedAsync(salesId!.Value, dto.ConsultantId))
            return (false, "Sales recruiter can only submit assigned consultants.");
        if (!await _db.Consultants.AnyAsync(c => c.Id == dto.ConsultantId && c.OrganizationId == _orgId))
            return (false, "Consultant is required.");

        var proof = string.IsNullOrWhiteSpace(dto.ProofFilePath) ? s.ProofFilePath : dto.ProofFilePath;
        if (string.IsNullOrWhiteSpace(proof))
            return (false, "Submission proof file is required.");

        s.ConsultantId = dto.ConsultantId;
        s.VendorId = dto.VendorId;
        s.JobTitle = dto.JobTitle;
        s.ClientName = dto.ClientName;
        s.SubmissionDate = dto.SubmissionDate;
        s.Status = dto.Status;
        s.Rate = dto.Rate;
        s.Notes = dto.Notes;
        s.ProofFilePath = proof!;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int? Id)> CreateInterviewAsync(
        ClaimsPrincipal user, bool isElevated, CreateInterviewRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.InviteProofFilePath))
            return (false, "Interview invite proof file is required.", null);

        var sub = await _db.Submissions.Include(s => s.Consultant).FirstOrDefaultAsync(s => s.Id == dto.SubmissionId);
        if (sub is null) return (false, "Submission not found.", null);
        if (sub.Consultant.OrganizationId != _orgId) return (false, "Forbidden.", null);

        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        if (!isElevated && sub.SalesRecruiterId != salesId) return (false, "Forbidden.", null);

        var code = NewInterviewCode();
        while (await _db.Interviews.AnyAsync(i => i.InterviewCode == code))
            code = NewInterviewCode();

        if (dto.InterviewEndDate.HasValue && dto.InterviewEndDate.Value < dto.InterviewDate)
            return (false, "Interview end time must be on or after the start time.", null);

        var interview = new Interview
        {
            InterviewCode = code,
            SubmissionId = dto.SubmissionId,
            InterviewDate = dto.InterviewDate,
            InterviewEndDate = dto.InterviewEndDate,
            InterviewMode = dto.InterviewMode,
            Round = dto.Round,
            Status = dto.Status,
            Feedback = dto.Feedback,
            Notes = dto.Notes,
            InviteProofFilePath = dto.InviteProofFilePath,
            CreatedAt = DateTime.UtcNow
        };
        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync();
        return (true, null, interview.Id);
    }

    public async Task<IReadOnlyList<InterviewDto>> GetInterviewsAsync(ClaimsPrincipal user, bool isElevated)
    {
        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        if (!isElevated && !salesId.HasValue) return Array.Empty<InterviewDto>();

        return await _db.Interviews.AsNoTracking()
            .Where(i => i.Submission.Consultant.OrganizationId == _orgId &&
                (isElevated || i.Submission.SalesRecruiterId == salesId))
            .OrderByDescending(i => i.InterviewDate)
            .Select(i => new InterviewDto
            {
                Id = i.Id,
                InterviewCode = i.InterviewCode,
                SubmissionId = i.SubmissionId,
                SubmissionCode = i.Submission.SubmissionCode,
                ConsultantName = i.Submission.Consultant.FirstName + " " + i.Submission.Consultant.LastName,
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

    public async Task<(bool Success, string? Error)> UpdateInterviewAsync(
        ClaimsPrincipal user, bool isElevated, int id, CreateInterviewRequestDto dto)
    {
        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        var i = await _db.Interviews.Include(x => x.Submission).ThenInclude(s => s.Consultant).FirstOrDefaultAsync(x => x.Id == id);
        if (i is null) return (false, "Interview not found.");
        if (i.Submission.Consultant.OrganizationId != _orgId) return (false, "Forbidden.");
        if (!isElevated && i.Submission.SalesRecruiterId != salesId) return (false, "Forbidden.");

        var sub = await _db.Submissions.Include(s => s.Consultant).FirstOrDefaultAsync(s => s.Id == dto.SubmissionId);
        if (sub is null) return (false, "Submission not found.");
        if (sub.Consultant.OrganizationId != _orgId) return (false, "Forbidden.");
        if (!isElevated && sub.SalesRecruiterId != salesId) return (false, "Forbidden.");

        if (dto.InterviewEndDate.HasValue && dto.InterviewEndDate.Value < dto.InterviewDate)
            return (false, "Interview end time must be on or after the start time.");

        i.SubmissionId = dto.SubmissionId;
        i.InterviewDate = dto.InterviewDate;
        i.InterviewEndDate = dto.InterviewEndDate;
        i.InterviewMode = dto.InterviewMode;
        i.Round = dto.Round;
        i.Status = dto.Status;
        i.Feedback = dto.Feedback;
        i.Notes = dto.Notes;
        if (!string.IsNullOrWhiteSpace(dto.InviteProofFilePath))
            i.InviteProofFilePath = dto.InviteProofFilePath;
        if (string.IsNullOrWhiteSpace(i.InviteProofFilePath))
            return (false, "Interview invite proof file is required.");
        i.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, string? PhysicalPath, string DownloadFileName)> GetProofDownloadAsync(
        ClaimsPrincipal user, bool isElevated, string kind, int id)
    {
        var k = kind.Trim().ToLowerInvariant();
        var userId = UserContextHelper.GetUserId(user);
        var salesId = await GetSalesRecruiterIdAsync(userId);
        string? rel = null;
        string? preferredName = null;

        switch (k)
        {
            case "vendor":
                var v = await _db.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (v is null) return (false, "Vendor not found.", null, string.Empty);
                if (v.OrganizationId != _orgId) return (false, "Forbidden.", null, string.Empty);
                if (!isElevated)
                {
                    if (!salesId.HasValue || v.SalesRecruiterId != salesId)
                        return (false, "Forbidden.", null, string.Empty);
                }

                rel = v.ContactProofFilePath;
                preferredName = $"{v.VendorCode}-contact{Path.GetExtension(rel ?? "")}";
                break;
            case "submission":
                var s = await _db.Submissions.AsNoTracking()
                    .Include(x => x.Consultant)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (s is null) return (false, "Submission not found.", null, string.Empty);
                if (s.Consultant.OrganizationId != _orgId) return (false, "Forbidden.", null, string.Empty);
                if (!isElevated && s.SalesRecruiterId != salesId) return (false, "Forbidden.", null, string.Empty);
                rel = s.ProofFilePath;
                preferredName = $"{s.SubmissionCode}-proof{Path.GetExtension(rel ?? "")}";
                break;
            case "interview":
                var i = await _db.Interviews.AsNoTracking()
                    .Include(x => x.Submission)
                    .ThenInclude(s => s.Consultant)
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (i is null) return (false, "Interview not found.", null, string.Empty);
                if (i.Submission.Consultant.OrganizationId != _orgId) return (false, "Forbidden.", null, string.Empty);
                if (!isElevated && i.Submission.SalesRecruiterId != salesId) return (false, "Forbidden.", null, string.Empty);
                rel = i.InviteProofFilePath;
                preferredName = $"{i.InterviewCode}-invite{Path.GetExtension(rel ?? "")}";
                break;
            default:
                return (false, "Invalid kind. Use vendor, submission, or interview.", null, string.Empty);
        }

        var (ok, err, path, name) = WwwrootFileResolver.TryResolve(_env.ContentRootPath, rel, preferredName);
        if (!ok) return (false, err, null, string.Empty);
        return (true, null, path, name);
    }
}
