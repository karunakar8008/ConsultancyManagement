namespace ConsultancyManagement.Core.Entities;

public class Consultant
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? VisaStatus { get; set; }
    public string? Technology { get; set; }
    /// <summary>Additional technologies, skills, or stack notes (editable by admin).</summary>
    public string? SkillsNotes { get; set; }
    public int? ExperienceYears { get; set; }
    public string? CurrentLocation { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<ConsultantSalesAssignment> SalesAssignments { get; set; } = new List<ConsultantSalesAssignment>();
    public ICollection<DailyActivity> DailyActivities { get; set; } = new List<DailyActivity>();
    public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<OnboardingTask> OnboardingTasks { get; set; } = new List<OnboardingTask>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<ConsultantVendorReachOut> VendorReachOuts { get; set; } = new List<ConsultantVendorReachOut>();
}
