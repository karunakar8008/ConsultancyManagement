namespace ConsultancyManagement.Core.Enums;

/// <summary>Application roles aligned with ASP.NET Identity role names.</summary>
public enum UserRole
{
    /// <summary>Can create organizations and bootstrap org admins (software owner).</summary>
    PlatformAdmin,
    Admin,
    Management,
    SalesRecruiter,
    Consultant
}
