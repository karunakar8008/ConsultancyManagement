using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ConsultancyManagement.Infrastructure.Data;
using ConsultancyManagement.Infrastructure.Services;

namespace ConsultancyManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        var resolvedConnectionString = !string.IsNullOrWhiteSpace(databaseUrl)
            ? databaseUrl
            : configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(resolvedConnectionString))
        {
            throw new InvalidOperationException("Database connection is not configured. Use DATABASE_URL or ConnectionStrings:DefaultConnection.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(resolvedConnectionString));
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddHttpContextAccessor();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ICurrentOrganization, CurrentOrganization>();
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IConsultantPortalService, ConsultantPortalService>();
        services.AddScoped<ISalesPortalService, SalesPortalService>();
        services.AddScoped<IManagementPortalService, ManagementPortalService>();
        services.AddScoped<IReportsService, ReportsService>();
        services.AddScoped<IDirectoryService, DirectoryService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPlatformService, PlatformService>();

        return services;
    }
}
