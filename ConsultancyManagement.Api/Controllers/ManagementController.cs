using ConsultancyManagement.Api.DTOs;
using ConsultancyManagement.Api.Helpers;
using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/management")]
[Authorize(Roles = $"{nameof(UserRole.Management)},{nameof(UserRole.Admin)}")]
public class ManagementController : ControllerBase
{
    private readonly IManagementPortalService _svc;
    private readonly IReportsService _reports;
    private readonly IConsultantPortalService _consultantPortal;

    public ManagementController(
        IManagementPortalService svc,
        IReportsService reports,
        IConsultantPortalService consultantPortal)
    {
        _svc = svc;
        _reports = reports;
        _consultantPortal = consultantPortal;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok(await _svc.GetDashboardAsync());

    [HttpGet("consultants")]
    public async Task<IActionResult> Consultants() => Ok(await _svc.GetConsultantsAsync());

    [HttpGet("consultants/{id:int}/activities")]
    public async Task<IActionResult> Activities(int id) => Ok(await _svc.GetConsultantActivitiesAsync(id));

    [HttpGet("submissions")]
    public async Task<IActionResult> Submissions() => Ok(await _svc.GetSubmissionsAsync());

    [HttpGet("interviews")]
    public async Task<IActionResult> Interviews() => Ok(await _reports.GetInterviewsReportAsync());

    [HttpGet("onboarding")]
    public async Task<IActionResult> Onboarding() => Ok(await _svc.GetOnboardingTasksAsync());

    [HttpPost("onboarding/tasks")]
    public async Task<IActionResult> CreateTask([FromBody] CreateOnboardingTaskRequestDto dto)
    {
        var (ok, err, id) = await _svc.CreateOnboardingTaskAsync(dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Onboarding task created successfully", id });
    }

    [HttpPut("onboarding/tasks/{id:int}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] CreateOnboardingTaskRequestDto dto)
    {
        var (ok, err) = await _svc.UpdateOnboardingTaskAsync(id, dto);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "Onboarding task updated successfully" });
    }

    [HttpGet("documents")]
    public async Task<IActionResult> Documents() => Ok(await _svc.GetDocumentsAsync());

    /// <summary>All consultant documents plus sales proofs (vendor contact, submission, interview invite), newest first.</summary>
    [HttpGet("file-catalog")]
    public async Task<IActionResult> FileCatalog() => Ok(await _svc.GetFileCatalogAsync());

    [HttpGet("file-catalog/download")]
    public async Task<IActionResult> DownloadCatalogFile(
        [FromQuery] string kind,
        [FromQuery] int id,
        [FromQuery] bool inline = false)
    {
        var (ok, err, physicalPath, downloadName) = await _svc.GetFileCatalogDownloadAsync(kind, id);
        if (!ok) return NotFound(new { message = err });
        return new StreamedFileWithDispositionResult(physicalPath!, FileMimeHelper.GuessContentType(physicalPath!),
            downloadName, inline);
    }

    [HttpPost("consultants/{consultantId:int}/documents")]
    [RequestFormLimits(MultipartBodyLengthLimit = 21_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadConsultantDocument(
        int consultantId,
        [FromForm] UploadDocumentRequestDto dto)
    {
        if (dto.File is null || dto.File.Length == 0)
            return BadRequest(new { message = "A file is required." });

        await using var stream = dto.File.OpenReadStream();
        var (ok, err, doc) = await _consultantPortal.UploadDocumentAsync(
            User,
            isAdminOrManagement: true,
            consultantId,
            dto.DocumentType ?? string.Empty,
            stream,
            dto.File.FileName,
            dto.File.Length);

        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Document uploaded successfully", document = doc });
    }

    [HttpPut("documents/{id:int}/review")]
    public async Task<IActionResult> Review(int id, [FromBody] ReviewDocumentRequestDto dto)
    {
        var userId = UserContextHelper.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var (ok, err) = await _svc.ReviewDocumentAsync(userId, id, dto);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "Document review saved successfully" });
    }
}
