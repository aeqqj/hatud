using System.ComponentModel.DataAnnotations;
using CampusGo.Web.Models;

namespace CampusGo.Web.DTOs;

public record RegisterDto(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MaxLength(50)] string StudentId,
    [Required, MaxLength(50)] string Password,
    [Required] UserRole Role
);

public record LoginDto(
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MaxLength(50)] string Password
);

public record AuthResponseDto(
    [Required, MaxLength(255)] string Token,
    [Required, MaxLength(255)] string UserId,
    [Required, MaxLength(150)] string FullName,
    [Required] UserRole Role
);