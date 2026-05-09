using System.Security.Claims;
using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface IConsultantPortalService
{
    Task<ConsultantDashboardDto?> GetDashboardAsync(ClaimsPrincipal user, bool isAdminOrManagement);
    Task<ConsultantProfileDto?> GetProfileAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId);
    Task<(bool Success, string? Error)> UpdateProfileContactAsync(ClaimsPrincipal user, UpdateConsultantContactRequestDto dto);

    Task<(bool Success, string? Error)> CreateDailyActivityAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, CreateDailyActivityRequestDto dto);
    Task<IReadOnlyList<DailyActivityDto>> GetDailyActivitiesAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId);
    Task<(bool Success, string? Error)> UpdateDailyActivityAsync(ClaimsPrincipal user, bool isAdminOrManagement, int activityId, CreateDailyActivityRequestDto dto);
    Task<(bool Success, string? Error)> PatchDailyActivityNotesAsync(ClaimsPrincipal user, int activityId, PatchDailyActivityNotesDto dto);
    Task<DailyActivitySuggestionsDto?> GetDailyActivitySuggestionsAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, DateTime activityDate);

    Task<(bool Success, string? Error)> CreateJobApplicationAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, CreateJobApplicationRequestDto dto);
    Task<IReadOnlyList<JobApplicationDto>> GetJobApplicationsAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId);
    Task<(bool Success, string? Error)> UpdateJobApplicationAsync(ClaimsPrincipal user, bool isAdminOrManagement, int id, CreateJobApplicationRequestDto dto);

    Task<IReadOnlyList<ConsultantSubmissionDto>> GetSubmissionsAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId);
    Task<(bool Success, string? Error)> UpdateSubmissionConsultantCommunicationAsync(
        ClaimsPrincipal user, int submissionId, UpdateConsultantSubmissionCommunicationDto dto);
    Task<IReadOnlyList<ConsultantInterviewDto>> GetConsultantInterviewsAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId);
    Task<(bool Success, string? Error)> UpdateConsultantInterviewAsync(ClaimsPrincipal user, int interviewId, UpdateConsultantInterviewDto dto);

    Task<IReadOnlyList<ConsultantVendorReachOutDto>> GetVendorReachOutsAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId);
    Task<(bool Success, string? Error)> CreateVendorReachOutAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, CreateConsultantVendorReachOutDto dto);
    Task<(bool Success, string? Error)> UpdateVendorReachOutAsync(
        ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId, int reachOutId, UpdateConsultantVendorReachOutDto dto);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(ClaimsPrincipal user, bool isAdminOrManagement, int? consultantId);

    Task<(bool Success, string? Error, DocumentDto? Doc)> UploadDocumentAsync(
        ClaimsPrincipal user,
        bool isAdminOrManagement,
        int? consultantId,
        string documentType,
        Stream fileStream,
        string originalFileName,
        long fileLength);

    Task<(bool Success, string? Error, string? PhysicalPath, string DownloadFileName)> GetDocumentDownloadAsync(
        ClaimsPrincipal user,
        bool isAdminOrManagement,
        int documentId);

    /// <summary>Download submission or interview invite proof for the logged-in consultant only. kind: submission | interview.</summary>
    Task<(bool Success, string? Error, string? PhysicalPath, string DownloadFileName)> GetConsultantProofDownloadAsync(
        ClaimsPrincipal user,
        string kind,
        int id);
}
