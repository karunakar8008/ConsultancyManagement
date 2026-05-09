using Microsoft.AspNetCore.Http;

namespace ConsultancyManagement.Api.DTOs;

public class SalesVendorFormDto
{
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Notes { get; set; }
    /// <summary>Required with <see cref="ContactProof"/> — file is stored under that consultant's folder.</summary>
    public int? ConsultantId { get; set; }
    public IFormFile? ContactProof { get; set; }
}

public class SalesSubmissionFormDto
{
    public int ConsultantId { get; set; }
    public int VendorId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = "Submitted";
    public decimal? Rate { get; set; }
    public string? Notes { get; set; }
    public IFormFile? ProofFile { get; set; }
}

public class SalesInterviewFormDto
{
    public int SubmissionId { get; set; }
    public DateTime InterviewDate { get; set; }
    public DateTime? InterviewEndDate { get; set; }
    public string? InterviewMode { get; set; }
    public string? Round { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Feedback { get; set; }
    public string? Notes { get; set; }
    public IFormFile? InviteProofFile { get; set; }
}
