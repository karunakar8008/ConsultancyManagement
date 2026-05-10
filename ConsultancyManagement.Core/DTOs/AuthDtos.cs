namespace ConsultancyManagement.Core.DTOs;

public class LoginRequestDto
{
    /// <summary>Organization slug from login screen (e.g. acme-consulting).</summary>
    public string OrganizationSlug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public int OrganizationId { get; set; }
    public string OrganizationSlug { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

public class RegisterUserRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class RegisterUserResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class CurrentUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public int OrganizationId { get; set; }
    public string OrganizationSlug { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
}

public class ForgotPasswordRequestDto
{
    public string OrganizationSlug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ResetUrlBase { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    public string OrganizationSlug { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
