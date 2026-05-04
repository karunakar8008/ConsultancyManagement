namespace ConsultancyManagement.Core.Entities;

public class Vendor
{
    public int Id { get; set; }
    /// <summary>Human-readable unique code (e.g. VEN-ABC12DEF34).</summary>
    public string VendorCode { get; set; } = string.Empty;
    /// <summary>Sales recruiter who owns this vendor record.</summary>
    public int? SalesRecruiterId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Notes { get; set; }
    /// <summary>Optional proof file (relative path under wwwroot).</summary>
    public string? ContactProofFilePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public SalesRecruiter? SalesRecruiter { get; set; }
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
