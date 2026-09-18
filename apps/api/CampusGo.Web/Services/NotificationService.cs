using CampusGo.Web.Data;
using CampusGo.Web.Models;

namespace CampusGo.Web.Services;

public class NotificationService(AppDbContext db)
{
    public async Task NotifyAsync(Guid userId, string message, NotificationType type)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            Type = type,
            ReadStatus = false
        });

        await db.SaveChangesAsync();
    }
}