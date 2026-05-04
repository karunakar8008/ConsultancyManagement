using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface IReportsService
{
    Task<DailySummaryReportDto?> GetDailySummaryAsync(DateTime date);
    Task<WeeklySummaryReportDto?> GetWeeklySummaryAsync(DateTime startDate, DateTime endDate);
    Task<IReadOnlyList<ConsultantPerformanceDto>> GetConsultantPerformanceAsync();
    Task<IReadOnlyList<SalesPerformanceDto>> GetSalesPerformanceAsync();
    Task<IReadOnlyList<SubmissionReportRowDto>> GetSubmissionsReportAsync();
    Task<IReadOnlyList<InterviewReportRowDto>> GetInterviewsReportAsync();
    Task<IReadOnlyList<OnboardingStatusReportDto>> GetOnboardingStatusAsync();
}
