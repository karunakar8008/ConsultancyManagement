namespace ConsultancyManagement.Core.DTOs;

public class CreateConsultantRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? VisaStatus { get; set; }
    public string? Technology { get; set; }
    public string? SkillsNotes { get; set; }
    public int? ExperienceYears { get; set; }
    public string? CurrentLocation { get; set; }
    public string Status { get; set; } = "Active";
}

public class CreateSalesRecruiterRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = "Active";
}

public class CreateManagementUserRequestDto
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string Status { get; set; } = "Active";
}

public class CreateAssignmentRequestDto
{
    public int ConsultantId { get; set; }
    public int SalesRecruiterId { get; set; }
    public DateTime StartDate { get; set; }
}

public class CreateSalesManagementAssignmentRequestDto
{
    public int SalesRecruiterId { get; set; }
    public int ManagementUserId { get; set; }
    public DateTime StartDate { get; set; }
}

public class CreateDailyActivityRequestDto
{
    public DateTime ActivityDate { get; set; }
    public int JobsAppliedCount { get; set; }
    public int VendorReachedOutCount { get; set; }
    public int VendorResponseCount { get; set; }
    public int SubmissionsCount { get; set; }
    public int InterviewCallsCount { get; set; }
    public string? Notes { get; set; }
}

public class CreateJobApplicationRequestDto
{
    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? ClientName { get; set; }
    public string? Source { get; set; }
    public string? JobUrl { get; set; }
    public DateTime AppliedDate { get; set; }
    public string Status { get; set; } = "Applied";
    public string? Notes { get; set; }
}

public class CreateVendorRequestDto
{
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Notes { get; set; }
    /// <summary>Required when uploading contact proof — files are stored under this consultant's folder.</summary>
    public int? LinkedConsultantId { get; set; }
}

public class CreateSubmissionRequestDto
{
    public int ConsultantId { get; set; }
    public int VendorId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = "Submitted";
    public decimal? Rate { get; set; }
    public string? Notes { get; set; }
    /// <summary>Relative path under wwwroot (set by API after file upload).</summary>
    public string ProofFilePath { get; set; } = string.Empty;
}

public class CreateInterviewRequestDto
{
    public int SubmissionId { get; set; }
    public DateTime InterviewDate { get; set; }
    public DateTime? InterviewEndDate { get; set; }
    public string? InterviewMode { get; set; }
    public string? Round { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Feedback { get; set; }
    public string? Notes { get; set; }
    /// <summary>Relative path under wwwroot (set by API after file upload).</summary>
    public string InviteProofFilePath { get; set; } = string.Empty;
}

public class UpdateConsultantContactRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class CreateConsultantVendorReachOutDto
{
    public DateTime ReachedDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? VendorResponseNotes { get; set; }
    public string? Notes { get; set; }
}

public class UpdateConsultantVendorReachOutDto
{
    public DateTime ReachedDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? VendorResponseNotes { get; set; }
    public string? Notes { get; set; }
}

public class PatchDailyActivityNotesDto
{
    public string? Notes { get; set; }
}

public class UpdateConsultantSubmissionCommunicationDto
{
    public string? ConsultantCommunication { get; set; }
}

public class UpdateConsultantInterviewDto
{
    public DateTime InterviewDate { get; set; }
    public DateTime? InterviewEndDate { get; set; }
    public string? InterviewMode { get; set; }
    public string? Round { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public string? Notes { get; set; }
}
