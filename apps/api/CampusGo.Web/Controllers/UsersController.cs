using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Helpers;
using CampusGo.Web.Models;
using CampusGo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await db.Users
            .Select(u => new UserDto(u.UserId, u.FullName, u.Email, u.StudentId, u.Role, u.CreatedAt))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        return Ok(new UserDto(user.UserId, user.FullName, user.Email, user.StudentId, user.Role, user.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
    {
        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            StudentId = dto.StudentId,
            Role = dto.Role
        };

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var result = new UserDto(user.UserId, user.FullName, user.Email, user.StudentId, user.Role, user.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { id = user.UserId }, result);
    }

    [HttpPost("Users/me/profile-picture")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB upload limit, before compression
    public async Task<ActionResult<UserDto>> UploadProfilePicture(IFormFile file, [FromServices] ImageUploadService imageService)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Only JPEG, PNG, or WEBP images are allowed." });
        }

        var userId = User.GetUserId();
        var user = await db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        using var stream = file.OpenReadStream();
        var url = await imageService.UploadProfilePictureAsync(stream, userId);

        user.ProfilePictureUrl = url;
        await db.SaveChangesAsync();

        return Ok(new UserDto(user.UserId, user.FullName, user.Email, user.StudentId, user.Role, user.CreatedAt));
    }
}