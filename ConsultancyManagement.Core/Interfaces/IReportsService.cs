using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface IReportsService
{
    Task<DailySummaryReportDto?> GetDailySummaryAsync(DateTime date, int? consultantId = null, int? salesRecruiterId = null);
    Task<WeeklySummaryReportDto?> GetWeeklySummaryAsync(DateTime startDate, DateTime endDate, int? consultantId = null, int? salesRecruiterId = null);
    Task<IReadOnlyList<ConsultantPerformanceDto>> GetConsultantPerformanceAsync(int? consultantId = null);
    Task<IReadOnlyList<SalesPerformanceDto>> GetSalesPerformanceAsync(int? salesRecruiterId = null);
    Task<IReadOnlyList<SubmissionReportRowDto>> GetSubmissionsReportAsync();
    Task<IReadOnlyList<InterviewReportRowDto>> GetInterviewsReportAsync();
    Task<IReadOnlyList<OnboardingStatusReportDto>> GetOnboardingStatusAsync();
}
