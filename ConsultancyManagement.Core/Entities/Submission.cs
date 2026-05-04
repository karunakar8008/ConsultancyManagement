namespace ConsultancyManagement.Core.Entities;

public class Submission
{
    public int Id { get; set; }
    public string SubmissionCode { get; set; } = string.Empty;
    public int ConsultantId { get; set; }
    public int SalesRecruiterId { get; set; }
    public int VendorId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = "Submitted";
    public decimal? Rate { get; set; }
    public string? Notes { get; set; }
    /// <summary>Proof file path (relative under wwwroot). Required for new submissions via API.</summary>
    public string? ProofFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Consultant Consultant { get; set; } = null!;
    public SalesRecruiter SalesRecruiter { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}
