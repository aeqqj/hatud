using System.ComponentModel.DataAnnotations;
using CampusGo.Web.Models;

namespace CampusGo.Web.DTOs;

public record CreateNotificationDto(
    [Required] Guid UserId,
    [Required, MaxLength(500)] string Message,
    [Required] NotificationType Type
);

public record NotificationDto(
    Guid NotificationId,
    Guid UserId,
    string Message,
    NotificationType Type,
    bool ReadStatus,
    DateTime CreatedAt
);