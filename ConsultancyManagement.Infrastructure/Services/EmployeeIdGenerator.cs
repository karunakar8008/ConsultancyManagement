using ConsultancyManagement.Core.Enums;

namespace ConsultancyManagement.Infrastructure.Services;

public static class EmployeeIdGenerator
{
    public static string GetPrefix(UserRole role) => role switch
    {
        UserRole.Admin => "ADM",
        UserRole.Management => "MGT",
        UserRole.SalesRecruiter => "SAL",
        UserRole.Consultant => "CON",
        _ => "USR"
    };

    public static string Build(string prefix, int number) => $"{prefix}{number:D3}";
}
