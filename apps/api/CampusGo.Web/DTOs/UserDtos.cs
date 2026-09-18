using System.ComponentModel.DataAnnotations;
using CampusGo.Web.Models;

namespace CampusGo.Web.DTOs;

public record CreateUserDto(
    [Required, MaxLength(150)] string FullName,
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MaxLength(50)] string StudentId,
    [Required] UserRole Role
);

public record UserDto(
    Guid UserId,
    string FullName,
    string Email,
    string StudentId,
    UserRole Role,
    DateTime CreatedAt
);