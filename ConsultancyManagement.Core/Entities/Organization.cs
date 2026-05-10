namespace ConsultancyManagement.Core.Entities;

public class Organization
{
    public int Id { get; set; }
    /// <summary>URL-safe identifier used at login (e.g. acme-consulting).</summary>
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
