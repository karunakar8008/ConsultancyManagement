using System.Security.Claims;
using ConsultancyManagement.Core.DTOs;

namespace ConsultancyManagement.Core.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Error, LoginResponseDto? Result)> LoginAsync(LoginRequestDto request);
    Task<(bool Success, string? Error, RegisterUserResponseDto? Result)> RegisterAsync(ClaimsPrincipal creator, RegisterUserRequestDto request);
    Task<CurrentUserDto?> GetCurrentUserAsync(ClaimsPrincipal user);
    Task<(bool Success, string? Error)> ForgotPasswordAsync(ForgotPasswordRequestDto request);
    Task<(bool Success, string? Error)> ResetPasswordAsync(ResetPasswordRequestDto request);
}
