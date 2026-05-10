using System.Security.Claims;
using System.Text;
using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Data;
using ConsultancyManagement.Infrastructure.Helpers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ConsultancyManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>Single client-safe message; details only in logs (no user/org enumeration).</summary>
    private const string LoginFailedMessage = "Invalid credentials.";

    public AuthService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IEmailService emailService,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<(bool Success, string? Error, LoginResponseDto? Result)> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationSlug))
        {
            _logger.LogWarning("Login rejected: missing organization slug");
            return (false, LoginFailedMessage, null);
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email))
        {
            _logger.LogWarning("Login rejected: invalid email format");
            return (false, LoginFailedMessage, null);
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login rejected: missing password");
            return (false, LoginFailedMessage, null);
        }

        var slug = OrganizationSlugHelper.Normalize(request.OrganizationSlug);
        var org = await _db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Slug == slug && o.IsActive);
        if (org is null)
        {
            _logger.LogWarning("Login failed: no active organization for slug {Slug}", slug);
            return (false, LoginFailedMessage, null);
        }

        var user = await FindUserInOrganizationAsync(request.Email, org.Id);
        if (user is null || !user.IsActive || user.IsDeleted)
        {
            _logger.LogWarning(
                "Login failed: no active user for email in organization {OrganizationId}",
                org.Id);
            return (false, LoginFailedMessage, null);
        }

        var pwd = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!pwd.Succeeded)
        {
            _logger.LogWarning(
                "Login failed: password check failed for user {UserId} (wrong password or locked out)",
                user.Id);
            return (false, LoginFailedMessage, null);
        }

        var (token, expiresIn) = await _jwtTokenService.CreateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return (true, null, new LoginResponseDto
        {
            Token = token,
            ExpiresIn = expiresIn,
            UserId = user.Id,
            EmployeeId = user.EmployeeId ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Roles = roles.ToList(),
            OrganizationId = org.Id,
            OrganizationSlug = org.Slug,
            OrganizationName = org.Name
        });
    }

    public async Task<(bool Success, string? Error, RegisterUserResponseDto? Result)> RegisterAsync(
        ClaimsPrincipal creator, RegisterUserRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email))
            return (false, "A valid email is required.", null);
        if (string.IsNullOrWhiteSpace(request.Password))
            return (false, "Password is required.", null);
        if (string.IsNullOrWhiteSpace(request.Role))
            return (false, "Role is required.", null);
        if (!ValidationHelper.TryParseRole(request.Role, out var roleEnum))
            return (false, "Role must be a valid application role.", null);
        if (roleEnum == UserRole.PlatformAdmin)
            return (false, "This role cannot be assigned here.", null);

        var creatorUser = await _userManager.GetUserAsync(creator);
        if (creatorUser is null || creatorUser.IsDeleted || !creatorUser.IsActive)
            return (false, "Unauthorized.", null);

        var organizationId = creatorUser.OrganizationId;
        var normReg = _userManager.NormalizeEmail(request.Email.Trim());
        if (await _userManager.Users.AnyAsync(u =>
                !u.IsDeleted && u.OrganizationId == organizationId && u.NormalizedEmail == normReg))
            return (false, "A user with this email already exists.", null);

        var nextEmployeeId = await GetNextEmployeeIdAsync(roleEnum, organizationId);

        var user = new ApplicationUser
        {
            OrganizationId = organizationId,
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmployeeId = nextEmployeeId
        };

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
            return (false, string.Join(" ", create.Errors.Select(e => e.Description)), null);

        await _userManager.AddToRoleAsync(user, roleEnum.ToString());
        await EnsureRoleProfileAsync(user, roleEnum);

        return (true, null, new RegisterUserResponseDto
        {
            Message = "User created successfully",
            UserId = user.Id
        });
    }

    private async Task EnsureRoleProfileAsync(ApplicationUser user, UserRole role)
    {
        switch (role)
        {
            case UserRole.Consultant:
                if (!await _db.Consultants.AnyAsync(c => c.UserId == user.Id))
                {
                    _db.Consultants.Add(new Consultant
                    {
                        OrganizationId = user.OrganizationId,
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                break;
            case UserRole.SalesRecruiter:
                if (!await _db.SalesRecruiters.AnyAsync(s => s.UserId == user.Id))
                {
                    _db.SalesRecruiters.Add(new SalesRecruiter
                    {
                        OrganizationId = user.OrganizationId,
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                break;
            case UserRole.Management:
                if (!await _db.ManagementUsers.AnyAsync(m => m.UserId == user.Id))
                {
                    _db.ManagementUsers.Add(new ManagementUser
                    {
                        OrganizationId = user.OrganizationId,
                        UserId = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                break;
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync();
        }
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null || user.IsDeleted) return null;
        var roles = await _userManager.GetRolesAsync(user);
        var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == user.OrganizationId);
        return new CurrentUserDto
        {
            UserId = user.Id,
            EmployeeId = user.EmployeeId ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Roles = roles.ToList(),
            OrganizationId = user.OrganizationId,
            OrganizationSlug = org?.Slug ?? string.Empty,
            OrganizationName = org?.Name ?? string.Empty
        };
    }

    public async Task<(bool Success, string? Error)> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationSlug))
            return (false, "Organization is required.");
        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email))
            return (false, "A valid email is required.");

        // Production/AWS: set PasswordReset:PublicResetUrlBase (HTTPS) so mail links always point at your real SPA
        // even if someone opens the site via IP or a non-canonical hostname.
        var configuredResetBase = _configuration["PasswordReset:PublicResetUrlBase"]?.Trim();
        var resetUrlBase = !string.IsNullOrWhiteSpace(configuredResetBase)
            ? configuredResetBase!
            : request.ResetUrlBase?.Trim();
        if (string.IsNullOrWhiteSpace(resetUrlBase))
            return (false, "ResetUrlBase is required.");

        var slug = OrganizationSlugHelper.Normalize(request.OrganizationSlug);
        var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Slug == slug && o.IsActive);
        if (org is null)
            return (true, null);

        var user = await FindUserInOrganizationAsync(request.Email, org.Id);
        if (user is null || !user.IsActive || user.IsDeleted)
        {
            // Do not disclose whether the email exists.
            return (true, null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var email = Uri.EscapeDataString(user.Email ?? string.Empty);
        var orgSlugEnc = Uri.EscapeDataString(org.Slug);
        var link = $"{resetUrlBase}?email={email}&token={Uri.EscapeDataString(encodedToken)}&organizationSlug={orgSlugEnc}";
        var body = $"""
                    <p>Hello {user.FirstName},</p>
                    <p>We received a request to reset your password.</p>
                    <p><a href="{link}">Click here to reset your password</a></p>
                    <p>If you did not request this, you can ignore this email.</p>
                    """;

        try
        {
            await _emailService.SendAsync(user.Email ?? request.Email.Trim(), "Reset your password", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Forgot password email failed for {Email}", user.Email);
            return (false, "Password reset email could not be sent. The mail server may be misconfigured.");
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationSlug))
            return (false, "Organization is required.");
        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email))
            return (false, "A valid email is required.");
        if (string.IsNullOrWhiteSpace(request.Token))
            return (false, "Token is required.");
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return (false, "New password is required.");

        var slug = OrganizationSlugHelper.Normalize(request.OrganizationSlug);
        var org = await _db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Slug == slug && o.IsActive);
        if (org is null) return (false, "Invalid reset request.");

        var user = await FindUserInOrganizationAsync(request.Email, org.Id);
        if (user is null || !user.IsActive || user.IsDeleted)
            return (false, "Invalid reset request.");

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            return (false, "Invalid token.");
        }

        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    private async Task<string> GetNextEmployeeIdAsync(UserRole role, int organizationId)
    {
        var prefix = EmployeeIdGenerator.GetPrefix(role);
        var maxId = await _db.Users
            .Where(u => u.OrganizationId == organizationId && !string.IsNullOrEmpty(u.EmployeeId) && u.EmployeeId.StartsWith(prefix))
            .Select(u => u.EmployeeId)
            .ToListAsync();

        int nextNumber = 1;
        if (maxId.Any())
        {
            var numbers = maxId.Select(id => int.TryParse(id.Substring(3), out var n) ? n : 0).Where(n => n > 0);
            if (numbers.Any())
            {
                nextNumber = numbers.Max() + 1;
            }
        }

        return EmployeeIdGenerator.Build(prefix, nextNumber);
    }

    private async Task<ApplicationUser?> FindUserInOrganizationAsync(string email, int organizationId)
    {
        var norm = _userManager.NormalizeEmail(email.Trim());
        return await _userManager.Users.FirstOrDefaultAsync(u =>
            u.OrganizationId == organizationId && u.NormalizedEmail == norm && !u.IsDeleted);
    }
}
