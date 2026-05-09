using System.Text;

namespace ConsultancyManagement.Infrastructure.Helpers;

/// <summary>
/// Builds the uploads subfolder segment for a consultant: sanitized display name + id (unique per consultant).
/// Must stay in sync with paths stored on document records.
/// </summary>
public static class ConsultantFolderNameHelper
{
    /// <summary>Returns e.g. <c>John_Doe_12</c> for folder <c>uploads/John_Doe_12/</c>.</summary>
    public static string BuildSegment(string firstName, string lastName, int consultantId)
    {
        var raw = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(raw)) raw = "Consultant";
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(Math.Min(raw.Length, 120));
        var lastWasSep = true;
        foreach (var ch in raw)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSep)
                {
                    sb.Append('_');
                    lastWasSep = true;
                }
                continue;
            }
            lastWasSep = false;
            if (invalid.Contains(ch) || ch < 32) sb.Append('_');
            else sb.Append(ch);
        }
        var s = sb.ToString().Trim('_');
        while (s.Contains("__", StringComparison.Ordinal))
            s = s.Replace("__", "_", StringComparison.Ordinal);
        if (string.IsNullOrEmpty(s)) s = "Consultant";
        if (s.Length > 100) s = s[..100].TrimEnd('_');
        return $"{s}_{consultantId}";
    }
}
