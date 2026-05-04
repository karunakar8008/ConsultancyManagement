namespace ConsultancyManagement.Core.Entities;

public class ConsultantSalesAssignment
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public int SalesRecruiterId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Consultant Consultant { get; set; } = null!;
    public SalesRecruiter SalesRecruiter { get; set; } = null!;
}
