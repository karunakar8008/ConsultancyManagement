namespace ConsultancyManagement.Core.Interfaces;

/// <summary>Resolves the authenticated user's organization from the JWT.</summary>
public interface ICurrentOrganization
{
    int OrganizationId { get; }
}
