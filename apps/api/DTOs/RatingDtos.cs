using System.ComponentModel.DataAnnotations;

namespace CampusGo.Web.DTOs;

public record CreateRatingDto(
    [Required] Guid BookingId,
    [Required] Guid RaterId,
    [Required, Range(1, 5)] int Score,
    [MaxLength(500)] string? Comment
);

public record RatingDto(
    Guid RatingId,
    Guid BookingId,
    Guid RaterId,
    Guid RateeId,
    int Score,
    string? Comment,
    DateTime CreatedAt
);