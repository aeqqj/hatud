using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Helpers;
using CampusGo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api")]
public class TripsController(AppDbContext db) : ControllerBase
{
    [HttpGet("Trips")]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetAll([FromQuery] TripStatus? status)
    {
        var query = db.Trips.AsQueryable();
        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        var trips = await query
            .Select(t => new TripDto(t.TripId, t.DriverId, t.VehicleId, t.Origin, t.Destination, t.DepartureTime, t.Fare, t.AvailableSeats, t.Status))
            .ToListAsync();

        return Ok(trips);
    }

    [HttpGet("Trips/{tripId}")]
    public async Task<ActionResult<TripDto>> GetById(Guid tripId)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip is null) return NotFound();

        return Ok(new TripDto(trip.TripId, trip.DriverId, trip.VehicleId, trip.Origin, trip.Destination, trip.DepartureTime, trip.Fare, trip.AvailableSeats, trip.Status));
    }

    [HttpGet("Users/{userId}/trips")]
    public async Task<ActionResult<IEnumerable<TripDto>>> GetByDriver(Guid userId)
    {
        var trips = await db.Trips
            .Where(t => t.DriverId == userId)
            .Select(t => new TripDto(t.TripId, t.DriverId, t.VehicleId, t.Origin, t.Destination, t.DepartureTime, t.Fare, t.AvailableSeats, t.Status))
            .ToListAsync();

        return Ok(trips);
    }

    [HttpPost("Trips")]
    public async Task<ActionResult<TripDto>> Create(CreateTripDto dto)
    {
        var driverId = User.GetUserId();
        var driver = await db.Users.FindAsync(driverId);
        if (driver is null || driver.Role == UserRole.Rider)
        {
            return BadRequest(new { message = "Only users registered as drivers can create trips." });
        }

        var vehicle = await db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle is null)
        {
            return NotFound(new { message = "No vehicle found with the given VehicleId." });
        }

        if (vehicle.UserId != driverId)
        {
            return BadRequest(new { message = "This vehicle does not belong to the specified driver." });
        }

        if (dto.AvailableSeats > vehicle.Capacity)
        {
            return BadRequest(new { message = $"AvailableSeats cannot exceed the vehicle's capacity of {vehicle.Capacity}." });
        }

        var trip = new Trip
        {
            DriverId = driverId,
            VehicleId = dto.VehicleId,
            Origin = dto.Origin,
            Destination = dto.Destination,
            DepartureTime = dto.DepartureTime,
            Fare = dto.Fare,
            AvailableSeats = dto.AvailableSeats,
            Status = TripStatus.Open
        };

        db.Trips.Add(trip);

        // Instantiate a new room everytime a trip is created.
        var chatRoom = new ChatRoom { TripId = trip.TripId };
        db.ChatRooms.Add(chatRoom);

        await db.SaveChangesAsync();

        var result = new TripDto(trip.TripId, trip.DriverId, trip.VehicleId, trip.Origin, trip.Destination, trip.DepartureTime, trip.Fare, trip.AvailableSeats, trip.Status);
        return CreatedAtAction(nameof(GetById), new { tripId = trip.TripId }, result);
    }

    [HttpPut("Trips/{tripId}")]
    public async Task<ActionResult<TripDto>> Update(Guid tripId, UpdateTripDto dto)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip is null) return NotFound();

        if (trip.DriverId != User.GetUserId())
        {
            return Forbid();
        }

        var vehicle = await db.Vehicles.FindAsync(trip.VehicleId);
        if (vehicle is not null && dto.AvailableSeats > vehicle.Capacity)
        {
            return BadRequest(new { message = $"AvailableSeats cannot exceed the vehicle's capacity of {vehicle.Capacity}." });
        }

        trip.Origin = dto.Origin;
        trip.Destination = dto.Destination;
        trip.DepartureTime = dto.DepartureTime;
        trip.Fare = dto.Fare;
        trip.AvailableSeats = dto.AvailableSeats;
        trip.Status = dto.Status;

        await db.SaveChangesAsync();

        return Ok(new TripDto(trip.TripId, trip.DriverId, trip.VehicleId, trip.Origin, trip.Destination, trip.DepartureTime, trip.Fare, trip.AvailableSeats, trip.Status));
    }

    [HttpDelete("Trips/{tripId}")]
    public async Task<IActionResult> Cancel(Guid tripId)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip is null) return NotFound();

        if (trip.DriverId != User.GetUserId())
        {
            return Forbid();
        }

        var hasActiveBookings = await db.Bookings
            .AnyAsync(b => b.TripId == tripId && b.Status == BookingStatus.Confirmed);

        if (hasActiveBookings)
        {
            return Conflict(new { message = "This trip has confirmed bookings and cannot be cancelled directly. Cancel the bookings first." });
        }

        trip.Status = TripStatus.Cancelled;
        await db.SaveChangesAsync();

        return NoContent();
    }
}