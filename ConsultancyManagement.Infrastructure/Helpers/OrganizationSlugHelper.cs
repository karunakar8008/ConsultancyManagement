using System.Text.RegularExpressions;

namespace ConsultancyManagement.Infrastructure.Helpers;

public static partial class OrganizationSlugHelper
{
    [GeneratedRegex(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?$", RegexOptions.Compiled)]
    private static partial Regex SlugPattern();

    public static string Normalize(string slug) =>
        string.IsNullOrWhiteSpace(slug) ? string.Empty : slug.Trim().ToLowerInvariant();

    public static bool IsValidSlug(string normalizedSlug) =>
        !string.IsNullOrEmpty(normalizedSlug)
        && normalizedSlug.Length >= 2
        && normalizedSlug.Length <= 64
        && SlugPattern().IsMatch(normalizedSlug);
}
