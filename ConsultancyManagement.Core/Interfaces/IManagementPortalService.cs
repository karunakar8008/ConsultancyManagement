using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface IManagementPortalService
{
    Task<ManagementDashboardDto> GetDashboardAsync();
    Task<IReadOnlyList<ConsultantListDto>> GetConsultantsAsync();
    Task<IReadOnlyList<SalesRecruiterListDto>> GetSalesRecruitersAsync();
    Task<IReadOnlyList<DailyActivityDto>> GetConsultantActivitiesAsync(int consultantId);
    Task<IReadOnlyList<SubmissionReportRowDto>> GetSubmissionsAsync();
    Task<IReadOnlyList<OnboardingTaskDto>> GetOnboardingTasksAsync();

    Task<(bool Success, string? Error, int? Id)> CreateOnboardingTaskAsync(CreateOnboardingTaskRequestDto dto);
    Task<(bool Success, string? Error)> UpdateOnboardingTaskAsync(int id, CreateOnboardingTaskRequestDto dto);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync();
    Task<IReadOnlyList<ManagementFileCatalogItemDto>> GetFileCatalogAsync();
    Task<(bool Success, string? Error, string? PhysicalPath, string DownloadFileName)> GetFileCatalogDownloadAsync(string kind, int id);
    Task<(bool Success, string? Error)> ReviewDocumentAsync(string reviewerUserId, int id, ReviewDocumentRequestDto dto);
}
