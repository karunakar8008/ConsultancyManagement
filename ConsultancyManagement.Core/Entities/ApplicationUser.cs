using Microsoft.AspNetCore.Identity;

namespace ConsultancyManagement.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string EmployeeId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-deleted accounts stay in the database so <see cref="EmployeeId"/> is never reused.</summary>
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
