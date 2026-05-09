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
[Route("api/consultant")]
[Authorize(Roles = $"{nameof(UserRole.Consultant)},{nameof(UserRole.Admin)},{nameof(UserRole.Management)}")]
public class ConsultantController : ControllerBase
{
    private readonly IConsultantPortalService _svc;

    public ConsultantController(IConsultantPortalService svc) => _svc = svc;

    private bool IsElevated => UserContextHelper.IsInAnyRole(User,
        UserRole.Admin.ToString(), UserRole.Management.ToString());

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var d = await _svc.GetDashboardAsync(User, IsElevated);
        if (d is null) return NotFound(new { message = "Consultant profile not found" });
        return Ok(d);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> Profile([FromQuery] int? consultantId)
    {
        var p = await _svc.GetProfileAsync(User, IsElevated, consultantId);
        if (p is null) return NotFound(new { message = "Consultant profile not found" });
        return Ok(p);
    }

    [HttpPut("profile/contact")]
    public async Task<IActionResult> UpdateProfileContact([FromBody] UpdateConsultantContactRequestDto dto)
    {
        var (ok, err) = await _svc.UpdateProfileContactAsync(User, dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Profile updated successfully" });
    }

    [HttpGet("daily-activity-suggestions")]
    public async Task<IActionResult> DailyActivitySuggestions(
        [FromQuery] DateTime activityDate,
        [FromQuery] int? consultantId)
    {
        var s = await _svc.GetDailyActivitySuggestionsAsync(User, IsElevated, consultantId, activityDate);
        if (s is null) return NotFound(new { message = "Consultant profile not found" });
        return Ok(s);
    }

    [HttpPost("daily-activities")]
    public async Task<IActionResult> CreateDaily([FromBody] CreateDailyActivityRequestDto dto, [FromQuery] int? consultantId)
    {
        var (ok, err) = await _svc.CreateDailyActivityAsync(User, IsElevated, consultantId, dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Daily activity saved successfully" });
    }

    [HttpGet("daily-activities")]
    public async Task<IActionResult> DailyActivities([FromQuery] int? consultantId) =>
        Ok(await _svc.GetDailyActivitiesAsync(User, IsElevated, consultantId));

    [HttpPut("daily-activities/{id:int}")]
    public async Task<IActionResult> UpdateDaily(int id, [FromBody] CreateDailyActivityRequestDto dto)
    {
        var (ok, err) = await _svc.UpdateDailyActivityAsync(User, IsElevated, id, dto);
        if (!ok) return err?.Contains("not found") == true ? NotFound(new { message = err }) : BadRequest(new { message = err });
        return Ok(new { message = "Daily activity updated successfully" });
    }

    /// <summary>Consultant-only: update notes on a daily activity row.</summary>
    [HttpPatch("daily-activities/{id:int}/notes")]
    public async Task<IActionResult> PatchDailyNotes(int id, [FromBody] PatchDailyActivityNotesDto dto)
    {
        var (ok, err) = await _svc.PatchDailyActivityNotesAsync(User, id, dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Notes saved." });
    }

    [HttpPost("job-applications")]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobApplicationRequestDto dto, [FromQuery] int? consultantId)
    {
        var (ok, err) = await _svc.CreateJobApplicationAsync(User, IsElevated, consultantId, dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Job application created successfully" });
    }

    [HttpGet("job-applications")]
    public async Task<IActionResult> Jobs([FromQuery] int? consultantId) =>
        Ok(await _svc.GetJobApplicationsAsync(User, IsElevated, consultantId));

    [HttpPut("job-applications/{id:int}")]
    public async Task<IActionResult> UpdateJob(int id, [FromBody] CreateJobApplicationRequestDto dto)
    {
        var (ok, err) = await _svc.UpdateJobApplicationAsync(User, IsElevated, id, dto);
        if (!ok) return err?.Contains("not found") == true ? NotFound(new { message = err }) : BadRequest(new { message = err });
        return Ok(new { message = "Job application updated successfully" });
    }

    [HttpGet("submissions")]
    public async Task<IActionResult> Submissions([FromQuery] int? consultantId) =>
        Ok(await _svc.GetSubmissionsAsync(User, IsElevated, consultantId));

    [HttpPut("submissions/{id:int}/consultant-communication")]
    public async Task<IActionResult> UpdateSubmissionCommunication(
        int id, [FromBody] UpdateConsultantSubmissionCommunicationDto dto)
    {
        var (ok, err) = await _svc.UpdateSubmissionConsultantCommunicationAsync(User, id, dto);
        if (!ok) return err == "Forbidden." ? StatusCode(403, new { message = err }) :
            err?.Contains("not found") == true ? NotFound(new { message = err }) :
            BadRequest(new { message = err });
        return Ok(new { message = "Communication saved." });
    }

    [HttpGet("interviews")]
    public async Task<IActionResult> Interviews([FromQuery] int? consultantId) =>
        Ok(await _svc.GetConsultantInterviewsAsync(User, IsElevated, consultantId));

    [HttpPut("interviews/{id:int}")]
    public async Task<IActionResult> UpdateInterview(int id, [FromBody] UpdateConsultantInterviewDto dto)
    {
        var (ok, err) = await _svc.UpdateConsultantInterviewAsync(User, id, dto);
        if (!ok) return err == "Forbidden." ? StatusCode(403, new { message = err }) :
            err?.Contains("not found") == true ? NotFound(new { message = err }) :
            BadRequest(new { message = err });
        return Ok(new { message = "Interview updated." });
    }

    [HttpGet("vendor-reach-outs")]
    public async Task<IActionResult> VendorReachOuts([FromQuery] int? consultantId) =>
        Ok(await _svc.GetVendorReachOutsAsync(User, IsElevated, consultantId));

    [HttpPost("vendor-reach-outs")]
    public async Task<IActionResult> CreateVendorReachOut(
        [FromBody] CreateConsultantVendorReachOutDto dto,
        [FromQuery] int? consultantId)
    {
        var (ok, err) = await _svc.CreateVendorReachOutAsync(User, IsElevated, consultantId, dto);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Vendor reach-out saved successfully" });
    }

    [HttpPut("vendor-reach-outs/{id:int}")]
    public async Task<IActionResult> UpdateVendorReachOut(
        int id,
        [FromBody] UpdateConsultantVendorReachOutDto dto,
        [FromQuery] int? consultantId)
    {
        var (ok, err) = await _svc.UpdateVendorReachOutAsync(User, IsElevated, consultantId, id, dto);
        if (!ok) return err?.Contains("not found") == true ? NotFound(new { message = err }) : BadRequest(new { message = err });
        return Ok(new { message = "Vendor reach-out updated." });
    }

    [HttpGet("documents")]
    public async Task<IActionResult> Documents([FromQuery] int? consultantId) =>
        Ok(await _svc.GetDocumentsAsync(User, IsElevated, consultantId));

    [HttpPost("documents")]
    [RequestFormLimits(MultipartBodyLengthLimit = 21_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] UploadDocumentRequestDto dto,
        [FromQuery] int? consultantId)
    {
        if (dto.File is null || dto.File.Length == 0)
            return BadRequest(new { message = "A file is required." });

        await using var stream = dto.File.OpenReadStream();
        var (ok, err, doc) = await _svc.UploadDocumentAsync(
            User,
            IsElevated,
            consultantId,
            dto.DocumentType ?? string.Empty,
            stream,
            dto.File.FileName,
            dto.File.Length);

        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Document uploaded successfully", document = doc });
    }

    [HttpGet("documents/{id:int}/download")]
    public async Task<IActionResult> DownloadDocument(int id, [FromQuery] bool inline = false)
    {
        var (ok, err, physicalPath, downloadName) = await _svc.GetDocumentDownloadAsync(User, IsElevated, id);
        if (!ok)
            return err == "Forbidden."
                ? StatusCode(403, new { message = err })
                : NotFound(new { message = err });
        return new StreamedFileWithDispositionResult(physicalPath!, FileMimeHelper.GuessContentType(physicalPath!),
            downloadName, inline);
    }

    /// <summary>Consultant-only: download proof for own submission or interview invite. kind=submission|interview</summary>
    [HttpGet("proofs/download")]
    public async Task<IActionResult> DownloadProof([FromQuery] string kind, [FromQuery] int id, [FromQuery] bool inline = false)
    {
        var (ok, err, physicalPath, downloadName) = await _svc.GetConsultantProofDownloadAsync(User, kind, id);
        if (!ok)
            return err == "Forbidden."
                ? StatusCode(403, new { message = err })
                : NotFound(new { message = err });
        return new StreamedFileWithDispositionResult(physicalPath!, FileMimeHelper.GuessContentType(physicalPath!),
            downloadName, inline);
    }
}
