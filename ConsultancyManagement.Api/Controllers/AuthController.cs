using System.Data.Common;
using ConsultancyManagement.Core.DTOs;
using ConsultancyManagement.Core.Enums;
using ConsultancyManagement.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    private static bool IsDatabaseFailure(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            if (e is DbException or DbUpdateException)
                return true;
        }
        return false;
    }

    private ObjectResult DatabaseUnavailable(Exception ex)
    {
        _logger.LogError(ex, "Database error on auth request");
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            message = "Sign-in is temporarily unavailable. Please try again later."
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var (ok, err, result) = await _authService.LoginAsync(request);
            if (!ok) return Unauthorized(new { message = err });
            return Ok(result);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            return DatabaseUnavailable(ex);
        }
    }

    [HttpPost("register")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequestDto request)
    {
        try
        {
            var (ok, err, result) = await _authService.RegisterAsync(User, request);
            if (!ok) return BadRequest(new { message = err });
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            return DatabaseUnavailable(ex);
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        try
        {
            var (ok, err) = await _authService.ForgotPasswordAsync(request);
            if (!ok) return BadRequest(new { message = err });
            return Ok(new { message = "If your email exists, a reset link has been sent." });
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            return DatabaseUnavailable(ex);
        }
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        try
        {
            var (ok, err) = await _authService.ResetPasswordAsync(request);
            if (!ok) return BadRequest(new { message = err });
            return Ok(new { message = "Password reset successful." });
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            return DatabaseUnavailable(ex);
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync(User);
            if (user is null) return Unauthorized(new { message = "Invalid token" });
            return Ok(user);
        }
        catch (Exception ex) when (IsDatabaseFailure(ex))
        {
            return DatabaseUnavailable(ex);
        }
    }
}
