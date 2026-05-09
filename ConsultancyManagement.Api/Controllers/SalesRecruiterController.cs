using ConsultancyManagement.Api.DTOs;
using ConsultancyManagement.Api.Helpers;
using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Data;
using ConsultancyManagement.Infrastructure.Helpers;
using ConsultancyManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(Roles = $"{nameof(UserRole.SalesRecruiter)},{nameof(UserRole.Admin)},{nameof(UserRole.Management)}")]
public class SalesRecruiterController : ControllerBase
{
    private const long MaxUploadBytes = 20 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg", ".txt"
    ];

    private readonly ISalesPortalService _svc;
    private readonly IHostEnvironment _env;
    private readonly ApplicationDbContext _db;

    public SalesRecruiterController(ISalesPortalService svc, IHostEnvironment env, ApplicationDbContext db)
    {
        _svc = svc;
        _env = env;
        _db = db;
    }

    private bool IsElevated => UserContextHelper.IsInAnyRole(User,
        UserRole.Admin.ToString(), UserRole.Management.ToString());

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var d = await _svc.GetDashboardAsync(User, IsElevated);
        if (d is null) return NotFound(new { message = "Sales recruiter profile not found" });
        return Ok(d);
    }

    [HttpGet("assigned-consultants")]
    public async Task<IActionResult> Assigned() => Ok(await _svc.GetAssignedConsultantsAsync(User, IsElevated));

    [HttpGet("assigned-consultants/{consultantId:int}")]
    public async Task<IActionResult> AssignedOne(int consultantId)
    {
        var c = await _svc.GetAssignedConsultantAsync(User, IsElevated, consultantId);
        if (c is null) return NotFound(new { message = "Consultant not found or not assigned" });
        return Ok(c);
    }

    [HttpPost("vendors")]
    [RequestFormLimits(MultipartBodyLengthLimit = 21_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateVendor([FromForm] SalesVendorFormDto form)
    {
        if (form.ContactProof is { Length: > 0 } && (!form.ConsultantId.HasValue || form.ConsultantId <= 0))
            return BadRequest(new { message = "Consultant is required when uploading a vendor contact proof (files are stored in that consultant's folder)." });

        var dto = new CreateVendorRequestDto
        {
            VendorName = form.VendorName,
            ContactPerson = form.ContactPerson,
            Email = form.Email,
            PhoneNumber = form.PhoneNumber,
            CompanyName = form.CompanyName,
            LinkedInUrl = form.LinkedInUrl,
            Notes = form.Notes,
            LinkedConsultantId = form.ConsultantId is > 0 ? form.ConsultantId : null
        };

        string? proofPath = null;
        if (form.ContactProof is { Length: > 0 })
        {
            var (saved, err, path) = await SaveSalesUploadAsync(form.ContactProof, dto.LinkedConsultantId);
            if (!saved) return BadRequest(new { message = err });
            proofPath = path;
        }

        var (ok, errMsg, id) = await _svc.CreateVendorAsync(User, IsElevated, dto, proofPath);
        if (!ok) return BadRequest(new { message = errMsg });
        return Ok(new { message = "Vendor created successfully", id });
    }

    [HttpGet("vendors")]
    public async Task<IActionResult> Vendors() => Ok(await _svc.GetVendorsAsync(User, IsElevated));

    [HttpPut("vendors/{id:int}")]
    [RequestFormLimits(MultipartBodyLengthLimit = 21_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateVendor(int id, [FromForm] SalesVendorFormDto form)
    {
        int? proofConsultantId = form.ConsultantId is > 0 ? form.ConsultantId : null;
        if (proofConsultantId is null && form.ContactProof is { Length: > 0 })
        {
            var prev = await _db.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            proofConsultantId = prev?.LinkedConsultantId;
        }
        if (form.ContactProof is { Length: > 0 } && (!proofConsultantId.HasValue || proofConsultantId <= 0))
            return BadRequest(new { message = "Consultant is required when uploading a vendor contact proof (or link a consultant on this vendor first)." });

        var dto = new CreateVendorRequestDto
        {
            VendorName = form.VendorName,
            ContactPerson = form.ContactPerson,
            Email = form.Email,
            PhoneNumber = form.PhoneNumber,
            CompanyName = form.CompanyName,
            LinkedInUrl = form.LinkedInUrl,
            Notes = form.Notes,
            LinkedConsultantId = form.ConsultantId is > 0 ? form.ConsultantId : null
        };

        string? proofPath = null;
        if (form.ContactProof is { Length: > 0 })
        {
            var (saved, err, path) = await SaveSalesUploadAsync(form.ContactProof, proofConsultantId);
            if (!saved) return BadRequest(new { message = err });
            proofPath = path;
        }

        var (ok, errMsg) = await _svc.UpdateVendorAsync(User, IsElevated, id, dto, proofPath);
        if (!ok)
        {
            if (errMsg == "Forbidden.") return StatusCode(403, new { message = errMsg });
            return errMsg?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { message = errMsg })
                : BadRequest(new { message = errMsg });
        }

        return Ok(new { message = "Vendor updated successfully" });
    }

    [HttpPost("submissions")]
    [RequestFormLimits(MultipartBodyLengthLimit = 21_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateSubmission([FromForm] SalesSubmissionFormDto form)
    {
        if (form.ProofFile is null || form.ProofFile.Length == 0)
            return BadRequest(new { message = "Submission proof file is required." });

        var (saved, saveErr, proofPath) = await SaveSalesUploadAsync(form.ProofFile, form.ConsultantId);
        if (!saved) return BadRequest(new { message = saveErr });

        var dto = new CreateSubmissionRequestDto
        {
            ConsultantId = form.ConsultantId,
            VendorId = form.VendorId,
            JobTitle = form.JobTitle,
            ClientName = form.ClientName,
            SubmissionDate = form.SubmissionDate,
            Status = form.Status,
            Rate = form.Rate,
            Notes = form.Notes,
            ProofFilePath = proofPath!
        };

        var (ok, err, id) = await _svc.CreateSubmissionAsync(User, IsElevated, dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Submission created successfully", id });
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> Submissions() => Ok(await _svc.GetSubmissionsAsync(User, IsElevated));

    [HttpGet("submission-options")]
    public async Task<IActionResult> SubmissionOptions() => Ok(await _svc.GetSubmissionOptionsAsync(User, IsElevated));

    [HttpGet("submissions/{id:int}")]
    public async Task<IActionResult> Submission(int id)
    {
        var s = await _svc.GetSubmissionByIdAsync(User, IsElevated, id);
        if (s is null) return NotFound(new { message = "Submission not found" });
        return Ok(s);
    }

    [HttpPut("submissions/{id:int}")]
    [RequestFormLimits(MultipartBodyLengthLimit = 21_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateSubmission(int id, [FromForm] SalesSubmissionFormDto form)
    {
        string? proofPath = null;
        if (form.ProofFile is { Length: > 0 })
        {
            var (saved, saveErr, path) = await SaveSalesUploadAsync(form.ProofFile, form.ConsultantId);
            if (!saved) return BadRequest(new { message = saveErr });
            proofPath = path;
        }

        var dto = new CreateSubmissionRequestDto
        {
            ConsultantId = form.ConsultantId,
            VendorId = form.VendorId,
            JobTitle = form.JobTitle,
            ClientName = form.ClientName,
            SubmissionDate = form.SubmissionDate,
            Status = form.Status,
            Rate = form.Rate,
            Notes = form.Notes,
            ProofFilePath = proofPath ?? string.Empty
        };

        var (ok, err) = await _svc.UpdateSubmissionAsync(User, IsElevated, id, dto);
        if (!ok)
        {
            if (err == "Forbidden.") return StatusCode(403, new { message = err });
            return err?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true
                ? NotFound(new { message = err })
                : BadRequest(new { message = err });
        }

        return Ok(new { message = "Submission updated successfully" });
    }

    [HttpPost("interviews")]
    [RequestFormLimits(MultipartBodyLengthLimit = 21_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateInterview([FromForm] SalesInterviewFormDto form)
    {
        if (form.InviteProofFile is null || form.InviteProofFile.Length == 0)
            return BadRequest(new { message = "Interview invite proof file is required." });

        var submission = await _db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == form.SubmissionId);
        if (submission is null) return BadRequest(new { message = "Submission not found." });

        var (saved, saveErr, invitePath) = await SaveSalesUploadAsync(form.InviteProofFile, submission.ConsultantId);
        if (!saved) return BadRequest(new { message = saveErr });

        var dto = new CreateInterviewRequestDto
        {
            SubmissionId = form.SubmissionId,
            InterviewDate = form.InterviewDate,
            InterviewEndDate = form.InterviewEndDate,
            InterviewMode = form.InterviewMode,
            Round = form.Round,
            Status = form.Status,
            Feedback = form.Feedback,
            Notes = form.Notes,
            InviteProofFilePath = invitePath!
        };

        var (ok, err, id) = await _svc.CreateInterviewAsync(User, IsElevated, dto);
        if (!ok)
        {
            if (err == "Forbidden.") return StatusCode(403, new { message = err });
            return BadRequest(new { message = err });
        }

        return Ok(new { message = "Interview scheduled successfully", id });
    }

    [HttpGet("interviews")]
    public async Task<IActionResult> Interviews() => Ok(await _svc.GetInterviewsAsync(User, IsElevated));

    /// <summary>Download proof file for a vendor, submission, or interview you own (or any if elevated). kind=vendor|submission|interview</summary>
    [HttpGet("proofs/download")]
    public async Task<IActionResult> DownloadProof(
        [FromQuery] string kind,
        [FromQuery] int id,
        [FromQuery] bool inline = false)
    {
        var (ok, err, physicalPath, downloadName) = await _svc.GetProofDownloadAsync(User, IsElevated, kind, id);
        if (!ok)
        {
            if (err == "Forbidden.") return StatusCode(403, new { message = err });
            return NotFound(new { message = err });
        }

        return new StreamedFileWithDispositionResult(physicalPath!, FileMimeHelper.GuessContentType(physicalPath!),
            downloadName, inline);
    }

    [HttpPut("interviews/{id:int}")]
    [RequestFormLimits(MultipartBodyLengthLimit = 21_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateInterview(int id, [FromForm] SalesInterviewFormDto form)
    {
        string? invitePath = null;
        if (form.InviteProofFile is { Length: > 0 })
        {
            var submission = await _db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == form.SubmissionId);
            if (submission is null) return BadRequest(new { message = "Submission not found." });
            var (saved, saveErr, path) = await SaveSalesUploadAsync(form.InviteProofFile, submission.ConsultantId);
            if (!saved) return BadRequest(new { message = saveErr });
            invitePath = path;
        }

        var dto = new CreateInterviewRequestDto
        {
            SubmissionId = form.SubmissionId,
            InterviewDate = form.InterviewDate,
            InterviewEndDate = form.InterviewEndDate,
            InterviewMode = form.InterviewMode,
            Round = form.Round,
            Status = form.Status,
            Feedback = form.Feedback,
            Notes = form.Notes,
            InviteProofFilePath = invitePath ?? string.Empty
        };

        var (ok, err) = await _svc.UpdateInterviewAsync(User, IsElevated, id, dto);
        if (!ok)
        {
            if (err == "Forbidden.") return StatusCode(403, new { message = err });
            return NotFound(new { message = err });
        }

        return Ok(new { message = "Interview updated successfully" });
    }

    /// <summary>Stores vendor, submission, and interview proofs under uploads/{consultant}/proofs/.</summary>
    private async Task<(bool Ok, string? Error, string? RelativePath)> SaveSalesUploadAsync(
        IFormFile file, int? consultantId)
    {
        if (file.Length <= 0 || file.Length > MaxUploadBytes)
            return (false, "File is empty or too large (max 20 MB).", null);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return (false, "File type not allowed. Use PDF, Word, images, or TXT.", null);

        if (!consultantId.HasValue || consultantId <= 0)
            return (false, "Consultant is required for file storage path.", null);

        var c = await _db.Consultants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == consultantId.Value);
        if (c is null) return (false, "Consultant not found.", null);

        var folderSeg = ConsultantFolderNameHelper.BuildSegment(c.FirstName, c.LastName, c.Id);
        var safeName = Path.GetFileName(file.FileName);
        var unique = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{safeName}";
        const string proofFolder = "proofs";
        var relativeDir = Path.Combine("uploads", folderSeg, proofFolder);
        var wwwroot = Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(wwwroot, relativeDir);
        Directory.CreateDirectory(dir);
        var relativePath = Path.Combine(relativeDir, unique).Replace('\\', '/');
        var physicalPath = Path.Combine(wwwroot, relativePath);

        await using (var output = new FileStream(physicalPath, FileMode.Create, FileAccess.Write))
        await using (var input = file.OpenReadStream())
        {
            await input.CopyToAsync(output);
        }

        return (true, null, relativePath);
    }
}
