namespace ConsultancyManagement.Core.Entities;

public class Interview
{
    public int Id { get; set; }
    public string InterviewCode { get; set; } = string.Empty;
    public int SubmissionId { get; set; }
    public DateTime InterviewDate { get; set; }
    public string? InterviewMode { get; set; }
    public string? Round { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Feedback { get; set; }
    public string? Notes { get; set; }
    /// <summary>Invite proof file path (relative under wwwroot). Required for new interviews via API.</summary>
    public string? InviteProofFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Submission Submission { get; set; } = null!;
}
