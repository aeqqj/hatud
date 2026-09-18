using System.ComponentModel.DataAnnotations;

namespace CampusGo.Web.DTOs;

public record ChatRoomDto(
    Guid ChatRoomId,
    Guid TripId,
    DateTime CreatedAt
);

public record CreateMessageDto(
    [Required] Guid SenderId,
    [Required, MaxLength(1000)] string Content
);

public record MessageDto(
    Guid MessageId,
    Guid ChatRoomId,
    Guid SenderId,
    string Content,
    DateTime SentAt
);