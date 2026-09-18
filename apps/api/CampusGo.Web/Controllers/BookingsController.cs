using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Helpers;
using CampusGo.Web.Models;
using CampusGo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api")]
public class BookingsController(AppDbContext db, NotificationService notifications) : ControllerBase
{
    private static readonly GeometryFactory GeometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private static BookingDto ToDto(Booking b) => new(
        b.BookingId, b.TripId, b.RiderId,
        b.PickupPoint.Y, b.PickupPoint.X, b.PickupLabel,
        b.DropoffPoint.Y, b.DropoffPoint.X, b.DropoffLabel,
        b.Status, b.Fare, b.CreatedAt
    );

    [HttpGet("Bookings/{bookingId}")]
    public async Task<ActionResult<BookingDto>> GetById(Guid bookingId)
    {
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking is null) return NotFound();

        return Ok(ToDto(booking));
    }

    [HttpGet("Trips/{tripId}/bookings")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetByTrip(Guid tripId)
    {
        var bookings = await db.Bookings.Where(b => b.TripId == tripId).ToListAsync();
        return Ok(bookings.Select(ToDto));
    }

    [HttpGet("Users/{riderId}/bookings")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetByRider(Guid riderId)
    {
        var bookings = await db.Bookings.Where(b => b.RiderId == riderId).ToListAsync();
        return Ok(bookings.Select(ToDto));
    }

    [HttpPost("Bookings")]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingDto dto)
    {
        var riderId = User.GetUserId();

        var trip = await db.Trips.FindAsync(dto.TripId);
        if (trip is null) return NotFound(new { message = "No trip found with the given TripId." });

        if (trip.Status != TripStatus.Open)
        {
            return BadRequest(new { message = "This trip is not open for bookings." });
        }

        if (trip.AvailableSeats <= 0)
        {
            return Conflict(new { message = "No available seats on this trip." });
        }

        if (riderId == trip.DriverId)
        {
            return BadRequest(new { message = "A driver cannot book their own trip." });
        }

        var alreadyBooked = await db.Bookings.AnyAsync(b =>
            b.TripId == dto.TripId &&
            b.RiderId == riderId &&
            b.Status != BookingStatus.Cancelled &&
            b.Status != BookingStatus.Rejected);

        if (alreadyBooked)
        {
            return Conflict(new { message = "You already have an active booking on this trip." });
        }

        var booking = new Booking
        {
            TripId = dto.TripId,
            RiderId = riderId,
            PickupPoint = GeometryFactory.CreatePoint(new Coordinate(dto.PickupLongitude, dto.PickupLatitude)),
            PickupLabel = dto.PickupLabel,
            DropoffPoint = GeometryFactory.CreatePoint(new Coordinate(dto.DropoffLongitude, dto.DropoffLatitude)),
            DropoffLabel = dto.DropoffLabel,
            Status = BookingStatus.Pending,
            Fare = trip.Fare
        };

        trip.AvailableSeats -= 1;
        if (trip.AvailableSeats == 0)
        {
            trip.Status = TripStatus.Full;
        }

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        await notifications.NotifyAsync(
            trip.DriverId,
            $"New booking request from a rider on your {trip.Origin} → {trip.Destination} trip.",
            NotificationType.BookingRequested);

        return CreatedAtAction(nameof(GetById), new { bookingId = booking.BookingId }, ToDto(booking));
    }

    [HttpPatch("Bookings/{bookingId}/status")]
    public async Task<ActionResult<BookingDto>> UpdateStatus(Guid bookingId, UpdateBookingStatusDto dto)
    {
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking is null) return NotFound();

        var trip = await db.Trips.FindAsync(booking.TripId);
        if (trip is null) return NotFound();   // add this if it's missing

        var callerId = User.GetUserId();
        var isDriver = trip.DriverId == callerId;
        var isRider = booking.RiderId == callerId;

        if (!isDriver && !isRider)
        {
            return Forbid();
        }

        var driverOnlyStatuses = new[] { BookingStatus.Confirmed, BookingStatus.Rejected };
        if (driverOnlyStatuses.Contains(dto.Status) && !isDriver)
        {
            return Forbid();
        }

        var releasesSeat =
            (dto.Status == BookingStatus.Rejected || dto.Status == BookingStatus.Cancelled) &&
            booking.Status != BookingStatus.Rejected && booking.Status != BookingStatus.Cancelled;

        booking.Status = dto.Status;

        if (releasesSeat)
        {
            trip.AvailableSeats += 1;
            if (trip.Status == TripStatus.Full)
            {
                trip.Status = TripStatus.Open;
            }
        }

        await db.SaveChangesAsync();

        var notifyType = dto.Status switch
        {
            BookingStatus.Confirmed => NotificationType.BookingConfirmed,
            BookingStatus.Rejected => NotificationType.BookingRejected,
            BookingStatus.Cancelled => NotificationType.BookingCancelled,
            _ => (NotificationType?)null
        };

        if (notifyType is not null)
        {
            var recipientId = callerId == trip.DriverId ? booking.RiderId : trip.DriverId;
            await notifications.NotifyAsync(
                recipientId,
                $"Your booking status changed to {dto.Status}.",
                notifyType.Value);
        }

        return Ok(ToDto(booking));
    }
}