namespace ConsultancyManagement.Core.Entities;

public class SalesManagementAssignment
{
    public int Id { get; set; }
    public int SalesRecruiterId { get; set; }
    public int ManagementUserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SalesRecruiter SalesRecruiter { get; set; } = null!;
    public ManagementUser ManagementUser { get; set; } = null!;
}
