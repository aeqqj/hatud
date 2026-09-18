using System.ComponentModel.DataAnnotations;
using CampusGo.Web.Models;

namespace CampusGo.Web.DTOs;

public record CreateBookingDto(
    [Required] Guid TripId,
    [Required, Range(-90, 90)] double PickupLatitude,
    [Required, Range(-180, 180)] double PickupLongitude,
    [Required, MaxLength(200)] string PickupLabel,
    [Required, Range(-90, 90)] double DropoffLatitude,
    [Required, Range(-180, 180)] double DropoffLongitude,
    [Required, MaxLength(200)] string DropoffLabel
);

public record UpdateBookingStatusDto(
    [Required] BookingStatus Status
);

public record BookingDto(
    Guid BookingId,
    Guid TripId,
    Guid RiderId,
    double PickupLatitude,
    double PickupLongitude,
    string PickupLabel,
    double DropoffLatitude,
    double DropoffLongitude,
    string DropoffLabel,
    BookingStatus Status,
    decimal Fare,
    DateTime CreatedAt
);