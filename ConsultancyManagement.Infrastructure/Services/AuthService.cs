using System.Security.Claims;
using System.Text;
using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Entities;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ConsultancyManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;

    public AuthService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IEmailService emailService)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
    }

    public async Task<(bool Success, string? Error, LoginResponseDto? Result)> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email))
            return (false, "A valid email is required.", null);
        if (string.IsNullOrWhiteSpace(request.Password))
            return (false, "Password is required.", null);

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || user.IsDeleted)
            return (false, "Invalid credentials", null);

        var pwd = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!pwd.Succeeded)
            return (false, "Invalid credentials", null);

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
            Roles = roles.ToList()
        });
    }

    public async Task<(bool Success, string? Error, RegisterUserResponseDto? Result)> RegisterAsync(RegisterUserRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email))
            return (false, "A valid email is required.", null);
        if (string.IsNullOrWhiteSpace(request.Password))
            return (false, "Password is required.", null);
        if (string.IsNullOrWhiteSpace(request.Role))
            return (false, "Role is required.", null);
        if (!ValidationHelper.TryParseRole(request.Role, out var roleEnum))
            return (false, "Role must be a valid application role.", null);

        var normReg = _userManager.NormalizeEmail(request.Email.Trim());
        if (await _userManager.Users.AnyAsync(u => !u.IsDeleted && u.NormalizedEmail == normReg))
            return (false, "A user with this email already exists.", null);

        var nextEmployeeId = await GetNextEmployeeIdAsync(roleEnum);

        var user = new ApplicationUser
        {
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
        return new CurrentUserDto
        {
            UserId = user.Id,
            EmployeeId = user.EmployeeId ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Roles = roles.ToList()
        };
    }

    public async Task<(bool Success, string? Error)> ForgotPasswordAsync(ForgotPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email))
            return (false, "A valid email is required.");
        if (string.IsNullOrWhiteSpace(request.ResetUrlBase))
            return (false, "ResetUrlBase is required.");

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || user.IsDeleted)
        {
            // Do not disclose whether the email exists.
            return (true, null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var email = Uri.EscapeDataString(user.Email ?? string.Empty);
        var link = $"{request.ResetUrlBase}?email={email}&token={Uri.EscapeDataString(encodedToken)}";
        var body = $"""
                    <p>Hello {user.FirstName},</p>
                    <p>We received a request to reset your password.</p>
                    <p><a href="{link}">Click here to reset your password</a></p>
                    <p>If you did not request this, you can ignore this email.</p>
                    """;

        await _emailService.SendAsync(user.Email ?? request.Email.Trim(), "Reset your password", body);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !ValidationHelper.IsValidEmail(request.Email))
            return (false, "A valid email is required.");
        if (string.IsNullOrWhiteSpace(request.Token))
            return (false, "Token is required.");
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return (false, "New password is required.");

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
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

    private async Task<string> GetNextEmployeeIdAsync(UserRole role)
    {
        var prefix = EmployeeIdGenerator.GetPrefix(role);
        var maxId = await _db.Users
            .Where(u => !string.IsNullOrEmpty(u.EmployeeId) && u.EmployeeId.StartsWith(prefix))
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
}
