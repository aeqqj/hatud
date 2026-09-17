using System.ComponentModel.DataAnnotations;
using CampusGo.Web.Models;

namespace CampusGo.Web.DTOs;

public record CreateVehicleDto(
    [Required] Guid UserId,
    [Required, MaxLength(20)] string PlateNumber,
    [Required, MaxLength(100)] string Model,
    [Required, Range(1, 20)] int Capacity
);

public record UpdateVehicleDto(
    [Required, MaxLength(20)] string PlateNumber,
    [Required, MaxLength(100)] string Model,
    [Required, Range(1, 20)] int Capacity
);

public record VehicleDto(
    Guid VehicleId,
    Guid UserId,
    string PlateNumber,
    string Model,
    int Capacity
);