namespace ConsultancyManagement.Core.Entities;

public class JobApplication
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? ClientName { get; set; }
    public string? Source { get; set; }
    public string? JobUrl { get; set; }
    public DateTime AppliedDate { get; set; }
    public string Status { get; set; } = "Applied";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Consultant Consultant { get; set; } = null!;
}
