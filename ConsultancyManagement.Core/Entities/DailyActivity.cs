namespace ConsultancyManagement.Core.Entities;

public class DailyActivity
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public DateTime ActivityDate { get; set; }
    public int JobsAppliedCount { get; set; }
    public int VendorReachedOutCount { get; set; }
    public int VendorResponseCount { get; set; }
    public int SubmissionsCount { get; set; }
    public int InterviewCallsCount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Consultant Consultant { get; set; } = null!;
}
