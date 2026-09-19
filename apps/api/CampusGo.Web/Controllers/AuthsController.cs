using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private static readonly PasswordHasher<User> Hasher = new();

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var emailExists = await db.Users.AnyAsync(u => u.Email == dto.Email);
        if (emailExists)
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            StudentId = dto.StudentId,
            Role = dto.Role
        };
        user.PasswordHash = Hasher.HashPassword(user, dto.Password);

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // The insert may have actually succeeded on a prior retry attempt,
            // with only the confirmation response lost to a flaky connection.
            // Re-fetch by email (unique, and known) rather than assume failure.
            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (existingUser is null)
            {
                // A genuine, different conflict — re-throw rather than mask it
                throw;
            }

            var recoveredToken = GenerateToken(existingUser);
            return Ok(new AuthResponseDto(recoveredToken, existingUser.UserId.ToString(), existingUser.FullName, existingUser.Role));
        }

        var token = GenerateToken(user);
        return Ok(new AuthResponseDto(token, user.UserId.ToString(), user.FullName, user.Role));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = GenerateToken(user);
        return Ok(new AuthResponseDto(token, user.UserId.ToString(), user.FullName, user.Role));
    }

    private string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpiryMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}