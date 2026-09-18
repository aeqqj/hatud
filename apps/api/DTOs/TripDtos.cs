using System.ComponentModel.DataAnnotations;
using CampusGo.Web.Models;

namespace CampusGo.Web.DTOs;

public record CreateTripDto(
    [Required] Guid DriverId,
    [Required] Guid VehicleId,
    [Required, MaxLength(200)] string Origin,
    [Required, MaxLength(200)] string Destination,
    [Required] DateTime DepartureTime,
    [Required, Range(0, 10000)] decimal Fare,
    [Required, Range(1, 20)] int AvailableSeats
);

public record UpdateTripDto(
    [Required, MaxLength(200)] string Origin,
    [Required, MaxLength(200)] string Destination,
    [Required] DateTime DepartureTime,
    [Required, Range(0, 10000)] decimal Fare,
    [Required, Range(1, 20)] int AvailableSeats,
    [Required] TripStatus Status
);

public record TripDto(
    Guid TripId,
    Guid DriverId,
    Guid VehicleId,
    string Origin,
    string Destination,
    DateTime DepartureTime,
    decimal Fare,
    int AvailableSeats,
    TripStatus Status
);