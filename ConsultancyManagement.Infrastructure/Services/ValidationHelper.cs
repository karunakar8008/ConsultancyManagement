using System.Net.Mail;
using ConsultancyManagement.Core.Enums;

namespace ConsultancyManagement.Infrastructure.Services;

public static class ValidationHelper
{
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParseRole(string role, out UserRole userRole)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            userRole = default;
            return false;
        }

        var normalized = role.Trim();
        return Enum.TryParse(normalized, ignoreCase: true, out userRole)
               && Enum.IsDefined(typeof(UserRole), userRole);
    }

    public static bool TryValidateEmployeeIdForRole(string employeeId, UserRole role, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(employeeId))
        {
            error = "Employee id is required.";
            return false;
        }

        var id = employeeId.Trim().ToUpperInvariant();
        var prefix = EmployeeIdGenerator.GetPrefix(role);
        if (!id.StartsWith(prefix, StringComparison.Ordinal))
        {
            error = $"Employee id must start with {prefix}.";
            return false;
        }

        var suffix = id[prefix.Length..];
        if (suffix.Length == 0 || !suffix.All(char.IsDigit))
        {
            error = "Employee id must end with one or more digits.";
            return false;
        }

        return true;
    }
}
