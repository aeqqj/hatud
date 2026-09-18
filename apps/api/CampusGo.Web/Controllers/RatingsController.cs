using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Helpers;
using CampusGo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api")]
public class RatingsController(AppDbContext db) : ControllerBase
{
    private static RatingDto ToDto(Rating r) =>
        new(r.RatingId, r.BookingId, r.RaterId, r.RateeId, r.Score, r.Comment, r.CreatedAt);

    [HttpGet("Bookings/{bookingId}/ratings")]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetByBooking(Guid bookingId)
    {
        var ratings = await db.Ratings.Where(r => r.BookingId == bookingId).ToListAsync();
        return Ok(ratings.Select(ToDto));
    }

    [HttpGet("Users/{userId}/ratings")]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetReceivedByUser(Guid userId)
    {
        var ratings = await db.Ratings.Where(r => r.RateeId == userId).ToListAsync();
        return Ok(ratings.Select(ToDto));
    }

    [HttpPost("Ratings")]
    public async Task<ActionResult<RatingDto>> Create(CreateRatingDto dto)
    {
        var raterId = User.GetUserId();

        var booking = await db.Bookings.FindAsync(dto.BookingId);
        if (booking is null) return NotFound(new { message = "No booking found with the given BookingId." });

        if (booking.Status != BookingStatus.Completed)
        {
            return BadRequest(new { message = "Ratings can only be left on completed bookings." });
        }

        var trip = await db.Trips.FindAsync(booking.TripId);
        if (trip is null) return NotFound(new { message = "Associated trip not found." });

        Guid rateeId;
        if (raterId == booking.RiderId)
        {
            rateeId = trip.DriverId;
        }
        else if (raterId == trip.DriverId)
        {
            rateeId = booking.RiderId;
        }
        else
        {
            return Forbid();
        }

        var alreadyRated = await db.Ratings.AnyAsync(r =>
            r.BookingId == dto.BookingId && r.RaterId == raterId);

        if (alreadyRated)
        {
            return Conflict(new { message = "You have already rated this booking." });
        }

        var rating = new Rating
        {
            BookingId = dto.BookingId,
            RaterId = raterId,
            RateeId = rateeId,
            Score = dto.Score,
            Comment = dto.Comment ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        db.Ratings.Add(rating);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByBooking), new { bookingId = rating.BookingId }, ToDto(rating));
    }
}