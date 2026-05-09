using ConsultancyManagement.Core.Interfaces;
using ConsultancyManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultancyManagement.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 50)
    {
        var userId = UserContextHelper.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        return Ok(await _notifications.GetForCurrentUserAsync(userId, take));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = UserContextHelper.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var count = await _notifications.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = UserContextHelper.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var (ok, err) = await _notifications.MarkReadAsync(userId, id);
        if (!ok) return NotFound(new { message = err });
        return Ok(new { message = "Marked read" });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = UserContextHelper.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        await _notifications.MarkAllReadAsync(userId);
        return Ok(new { message = "All marked read" });
    }
}
