using System.Security.Claims;
using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface ISalesPortalService
{
    Task<SalesDashboardDto?> GetDashboardAsync(ClaimsPrincipal user, bool isElevated);
    Task<IReadOnlyList<AssignedConsultantDto>> GetAssignedConsultantsAsync(ClaimsPrincipal user, bool isElevated);
    Task<AssignedConsultantDto?> GetAssignedConsultantAsync(ClaimsPrincipal user, bool isElevated, int consultantId);

    Task<(bool Success, string? Error, int? Id)> CreateVendorAsync(
        ClaimsPrincipal user, bool isElevated, CreateVendorRequestDto dto, string? contactProofRelativePath);
    Task<IReadOnlyList<VendorDto>> GetVendorsAsync(ClaimsPrincipal user, bool isElevated);
    Task<(bool Success, string? Error)> UpdateVendorAsync(
        ClaimsPrincipal user, bool isElevated, int id, CreateVendorRequestDto dto, string? contactProofRelativePath);

    Task<(bool Success, string? Error, int? Id)> CreateSubmissionAsync(ClaimsPrincipal user, bool isElevated, CreateSubmissionRequestDto dto);
    Task<IReadOnlyList<SalesSubmissionDto>> GetSubmissionsAsync(ClaimsPrincipal user, bool isElevated);
    Task<IReadOnlyList<SalesSubmissionOptionDto>> GetSubmissionOptionsAsync(ClaimsPrincipal user, bool isElevated);
    Task<SalesSubmissionDto?> GetSubmissionByIdAsync(ClaimsPrincipal user, bool isElevated, int id);
    Task<(bool Success, string? Error)> UpdateSubmissionAsync(ClaimsPrincipal user, bool isElevated, int id, CreateSubmissionRequestDto dto);

    Task<(bool Success, string? Error, int? Id)> CreateInterviewAsync(ClaimsPrincipal user, bool isElevated, CreateInterviewRequestDto dto);
    Task<IReadOnlyList<InterviewDto>> GetInterviewsAsync(ClaimsPrincipal user, bool isElevated);
    Task<(bool Success, string? Error)> UpdateInterviewAsync(ClaimsPrincipal user, bool isElevated, int id, CreateInterviewRequestDto dto);

    /// <summary>Download proof file: kind vendor | submission | interview (scoped to recruiter unless elevated).</summary>
    Task<(bool Success, string? Error, string? PhysicalPath, string DownloadFileName)> GetProofDownloadAsync(
        ClaimsPrincipal user, bool isElevated, string kind, int id);
}
