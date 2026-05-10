namespace ConsultancyManagement.Core.DTOs;

public class CreateOrganizationRequestDto
{
    public string Name { get; set; } = string.Empty;
    /// <summary>URL-safe code; lowercase letters, digits, hyphens.</summary>
    public string Slug { get; set; } = string.Empty;
}

public class OrganizationListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class BootstrapOrgAdminRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
