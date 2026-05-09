using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ConsultancyManagement.Infrastructure.Data;

/// <summary>
/// Lets <c>dotnet ef</c> create the DbContext at design time without starting the API host (avoids seeding/timeouts).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var apiDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "ConsultancyManagement.Api"));
        var config = new ConfigurationBuilder()
            .SetBasePath(apiDir)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config["DATABASE_URL"] ?? config.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("DATABASE_URL or ConnectionStrings:DefaultConnection is not configured.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        options.UseNpgsql(cs);
        return new ApplicationDbContext(options.Options);
    }
}
