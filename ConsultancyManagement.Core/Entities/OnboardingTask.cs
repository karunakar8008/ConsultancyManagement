namespace ConsultancyManagement.Core.Entities;

public class OnboardingTask
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Consultant Consultant { get; set; } = null!;
}
