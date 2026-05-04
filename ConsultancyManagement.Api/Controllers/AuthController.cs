using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var (ok, err, result) = await _authService.LoginAsync(request);
        if (!ok) return Unauthorized(new { message = err });
        return Ok(result);
    }

    [HttpPost("register")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequestDto request)
    {
        var (ok, err, result) = await _authService.RegisterAsync(request);
        if (!ok) return BadRequest(new { message = err });
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var (ok, err) = await _authService.ForgotPasswordAsync(request);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "If your email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var (ok, err) = await _authService.ResetPasswordAsync(request);
        if (!ok) return BadRequest(new { message = err });
        return Ok(new { message = "Password reset successful." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _authService.GetCurrentUserAsync(User);
        if (user is null) return Unauthorized(new { message = "Invalid token" });
        return Ok(user);
    }
}
