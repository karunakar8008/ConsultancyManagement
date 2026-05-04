namespace ConsultancyManagement.Core.DTOs;

public class AdminUserListDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}

public class AdminUserDetailDto : AdminUserListDto
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class CreateAdminUserRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    /// <summary>Optional employee id (e.g. CON003). When omitted, the next id for the role is assigned.</summary>
    public string? EmployeeId { get; set; }
}

public class UpdateAdminUserRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class RoleListDto
{
    public string Name { get; set; } = string.Empty;
}

public class ConsultantListDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmployeeId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Technology { get; set; }
    public string? SkillsNotes { get; set; }
    public string? VisaStatus { get; set; }
    public int? ExperienceYears { get; set; }
    public string? CurrentLocation { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SalesRecruiterListDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmployeeId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int AssignedConsultantsCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ManagementUserListDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmployeeId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AssignmentListDto
{
    public int Id { get; set; }
    public int ConsultantId { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public int SalesRecruiterId { get; set; }
    public string SalesRecruiterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class SalesManagementAssignmentListDto
{
    public int Id { get; set; }
    public int SalesRecruiterId { get; set; }
    public string SalesRecruiterName { get; set; } = string.Empty;
    public int ManagementUserId { get; set; }
    public string ManagementUserName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class DirectoryUserEntryDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class UpdateAssignmentRequestDto
{
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
