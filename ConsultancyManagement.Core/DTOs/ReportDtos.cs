namespace ConsultancyManagement.Core.DTOs;

public class DailySummaryReportDto
{
    public DateTime Date { get; set; }
    public int TotalJobsApplied { get; set; }
    public int TotalVendorReachOuts { get; set; }
    public int TotalVendorResponses { get; set; }
    public int TotalSubmissions { get; set; }
    public int TotalInterviewCalls { get; set; }
    public int ActiveConsultants { get; set; }
    /// <summary>When set, metrics are limited to this consultant.</summary>
    public int? ScopeConsultantId { get; set; }
    public string? ScopeConsultantName { get; set; }
    /// <summary>When set, activity totals are for consultants assigned to this recruiter; submissions/interviews attributed to this recruiter.</summary>
    public int? ScopeSalesRecruiterId { get; set; }
    public string? ScopeSalesRecruiterName { get; set; }
}

public class WeeklySummaryReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalJobsApplied { get; set; }
    public int TotalVendorReachOuts { get; set; }
    public int TotalVendorResponses { get; set; }
    public int TotalSubmissions { get; set; }
    public int TotalInterviews { get; set; }
    public int? ScopeConsultantId { get; set; }
    public string? ScopeConsultantName { get; set; }
    public int? ScopeSalesRecruiterId { get; set; }
    public string? ScopeSalesRecruiterName { get; set; }
}

public class ConsultantPerformanceDto
{
    public int ConsultantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int JobsApplied { get; set; }
    public int Submissions { get; set; }
    public int Interviews { get; set; }
}

public class SalesPerformanceDto
{
    public int SalesRecruiterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Submissions { get; set; }
    public int Interviews { get; set; }
    public int AssignedConsultants { get; set; }
}

public class SubmissionReportRowDto
{
    public int Id { get; set; }
    public string SubmissionCode { get; set; } = string.Empty;
    public string ConsultantName { get; set; } = string.Empty;
    public string SalesRecruiterName { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class InterviewReportRowDto
{
    public int Id { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime InterviewDate { get; set; }
    public DateTime? InterviewEndDate { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public string? Notes { get; set; }
}

public class OnboardingStatusReportDto
{
    public int ConsultantId { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
}
