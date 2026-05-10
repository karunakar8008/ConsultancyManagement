namespace ConsultancyManagement.Core.Entities;

public class SalesRecruiter
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<ConsultantSalesAssignment> ConsultantAssignments { get; set; } = new List<ConsultantSalesAssignment>();
    public ICollection<SalesManagementAssignment> ManagementAssignments { get; set; } = new List<SalesManagementAssignment>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();
}
