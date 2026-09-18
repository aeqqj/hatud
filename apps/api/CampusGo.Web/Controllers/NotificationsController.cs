using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Helpers;
using CampusGo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api")]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    private static NotificationDto ToDto(Notification n) =>
        new(n.NotificationId, n.UserId, n.Message, n.Type, n.ReadStatus, n.CreatedAt);

    [HttpGet("Users/me/notifications")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMine([FromQuery] bool? unreadOnly)
    {
        var userId = User.GetUserId();
        var query = db.Notifications.Where(n => n.UserId == userId);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.ReadStatus);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(notifications.Select(ToDto));
    }

    [HttpPatch("Notifications/{notificationId}/read")]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(Guid notificationId)
    {
        var notification = await db.Notifications.FindAsync(notificationId);
        if (notification is null) return NotFound();

        if (notification.UserId != User.GetUserId())
        {
            return Forbid();
        }

        notification.ReadStatus = true;
        await db.SaveChangesAsync();

        return Ok(ToDto(notification));
    }
}