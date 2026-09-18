namespace CampusGo.Web.Models;

public enum NotificationType
{
    TripReminder,
    BookingRequested,
    BookingConfirmed,
    BookingRejected,
    BookingCancelled,
    RatingRequest
}

public class Notification
{
    public Guid NotificationId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool ReadStatus { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}