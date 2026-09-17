using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
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

    [HttpGet("Users/{userId}/notifications")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetByUser(Guid userId, [FromQuery] bool? unreadOnly)
    {
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

    [HttpPost("Notifications")]
    public async Task<ActionResult<NotificationDto>> Create(CreateNotificationDto dto)
    {
        var userExists = await db.Users.AnyAsync(u => u.UserId == dto.UserId);
        if (!userExists)
        {
            return NotFound(new { message = "No user found with the given UserId." });
        }

        var notification = new Notification
        {
            UserId = dto.UserId,
            Message = dto.Message,
            Type = dto.Type,
            ReadStatus = false
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByUser), new { userId = notification.UserId }, ToDto(notification));
    }

    [HttpPatch("Notifications/{notificationId}/read")]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(Guid notificationId)
    {
        var notification = await db.Notifications.FindAsync(notificationId);
        if (notification is null) return NotFound();

        notification.ReadStatus = true;
        await db.SaveChangesAsync();

        return Ok(ToDto(notification));
    }
}