namespace ConsultancyManagement.Core.DTOs;

public class ConsultantDashboardDto
{
    public int JobsAppliedToday { get; set; }
    public int VendorsReachedOut { get; set; }
    public int VendorResponses { get; set; }
    public int Submissions { get; set; }
    public int InterviewCalls { get; set; }
}

public class SalesDashboardDto
{
    public int AssignedConsultants { get; set; }
    public int VendorsContactedToday { get; set; }
    public int SubmissionsToday { get; set; }
    public int InterviewsScheduled { get; set; }
}

public class ManagementDashboardDto
{
    public int TotalConsultants { get; set; }
    public int PendingOnboarding { get; set; }
    public int PendingDocuments { get; set; }
    public int TotalSubmissions { get; set; }
    public int InterviewsScheduled { get; set; }
}

public class AdminDashboardDto
{
    public int TotalConsultants { get; set; }
    public int TotalSalesRecruiters { get; set; }
    public int TotalManagementUsers { get; set; }
    public int TodayApplications { get; set; }
    public int TodaySubmissions { get; set; }
    public int PendingDocuments { get; set; }
}

public class DailyActivitySuggestionsDto
{
    public int JobsAppliedCount { get; set; }
    public int VendorReachedOutCount { get; set; }
    public int VendorResponseCount { get; set; }
    public int SubmissionsCount { get; set; }
    public int InterviewCallsCount { get; set; }
}

public class DailyActivityDto
{
    public int Id { get; set; }
    public DateTime ActivityDate { get; set; }
    public int JobsAppliedCount { get; set; }
    public int VendorReachedOutCount { get; set; }
    public int VendorResponseCount { get; set; }
    public int SubmissionsCount { get; set; }
    public int InterviewCallsCount { get; set; }
    public string? Notes { get; set; }
}

public class JobApplicationDto
{
    public int Id { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? ClientName { get; set; }
    public string? Source { get; set; }
    public string? JobUrl { get; set; }
    public DateTime AppliedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ConsultantSubmissionDto
{
    public int Id { get; set; }
    public string SubmissionCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string SalesRecruiterName { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    /// <summary>Consultant-only updates about vendor communication.</summary>
    public string? ConsultantCommunication { get; set; }
    public bool HasProof { get; set; }
}

public class ConsultantProfileDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? VisaStatus { get; set; }
    public string? Technology { get; set; }
    public string? SkillsNotes { get; set; }
    public int? ExperienceYears { get; set; }
    public string? CurrentLocation { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class VendorDto
{
    public int Id { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public int? SalesRecruiterId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Notes { get; set; }
    public bool HasContactProof { get; set; }
}

public class SalesSubmissionDto
{
    public int Id { get; set; }
    public string SubmissionCode { get; set; } = string.Empty;
    public int ConsultantId { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? Rate { get; set; }
    public string? Notes { get; set; }
    public bool HasProof { get; set; }
}

public class SalesSubmissionOptionDto
{
    public int Id { get; set; }
    public string SubmissionCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string ConsultantName { get; set; } = string.Empty;
}

public class InterviewDto
{
    public int Id { get; set; }
    public string InterviewCode { get; set; } = string.Empty;
    public int SubmissionId { get; set; }
    public string SubmissionCode { get; set; } = string.Empty;
    public string ConsultantName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime InterviewDate { get; set; }
    public DateTime? InterviewEndDate { get; set; }
    public string? InterviewMode { get; set; }
    public string? Round { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public string? Notes { get; set; }
    public bool HasInviteProof { get; set; }
}

public class ConsultantInterviewDto
{
    public int Id { get; set; }
    public string InterviewCode { get; set; } = string.Empty;
    public string SubmissionCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime InterviewDate { get; set; }
    public DateTime? InterviewEndDate { get; set; }
    public string? InterviewMode { get; set; }
    public string? Round { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public string? Notes { get; set; }
    public bool HasInviteProof { get; set; }
}

public class ConsultantVendorReachOutDto
{
    public int Id { get; set; }
    public DateTime ReachedDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? VendorResponseNotes { get; set; }
    public string? Notes { get; set; }
}

public class AssignedConsultantDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Technology { get; set; }
    public string? VisaStatus { get; set; }
    public string? CurrentLocation { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OnboardingTaskDto
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public class CreateOnboardingTaskRequestDto
{
    public int ConsultantId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "Pending";
}

public class DocumentDto
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    /// <summary>Folder segment under wwwroot/uploads for this consultant.</summary>
    public string StorageFolder { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    /// <summary>True when an admin review locked this document (non-admins cannot change review).</summary>
    public bool LockedAfterAdminDecision { get; set; }
    /// <summary>UserRole name of the last reviewer tier (Admin, Management, SalesRecruiter).</summary>
    public string? LastReviewAuthority { get; set; }
}

public class ReviewDocumentRequestDto
{
    public string Status { get; set; } = "Approved";
}

/// <summary>Unified file row for management: consultant uploads + sales proofs.</summary>
public class ManagementFileCatalogItemDto
{
    /// <summary>Document | VendorContactProof | SubmissionProof | InterviewInviteProof</summary>
    public string Kind { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ConsultantName { get; set; }
    /// <summary>Set for Kind Document: uploads subfolder name for that consultant.</summary>
    public string? ConsultantStorageFolder { get; set; }
    public string? SalesRecruiterName { get; set; }
    public string? VendorName { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool HasFile { get; set; }
    public DateTime At { get; set; }
}

/// <summary>Admin interview calendar: one row per interview in a date range.</summary>
public class InterviewCalendarEventDto
{
    public int Id { get; set; }
    public string InterviewCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string ConsultantName { get; set; } = string.Empty;
    public string SalesRecruiterName { get; set; } = string.Empty;
    public DateTime InterviewDate { get; set; }
    public DateTime? InterviewEndDate { get; set; }
    public string? InterviewMode { get; set; }
    public string Status { get; set; } = string.Empty;
}
