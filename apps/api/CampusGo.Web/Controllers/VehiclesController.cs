using System.Reflection.Metadata;
using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Helpers;
using CampusGo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api")]
public class VehiclesController(AppDbContext db) : ControllerBase
{
    [HttpGet("Vehicles")]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAll()
    {
        var vehicles = await db.Vehicles
            .Select(v => new VehicleDto(v.VehicleId, v.UserId, v.Type, v.PlateNumber, v.Model, v.Capacity))
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpGet("Vehicles/{vehicleId}")]
    public async Task<ActionResult<VehicleDto>> GetById(Guid vehicleId)
    {
        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        if (vehicle is null) return NotFound();

        return Ok(new VehicleDto(vehicle.VehicleId, vehicle.UserId, vehicle.Type, vehicle.PlateNumber, vehicle.Model, vehicle.Capacity));
    }

    [HttpGet("Users/{userId}/vehicles")]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehiclesOfDriver(Guid userId)
    {
        var vehicles = await db.Vehicles
            .Where(v => v.UserId == userId)
            .Select(v => new VehicleDto(v.VehicleId, v.UserId, v.Type, v.PlateNumber, v.Model, v.Capacity))
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpPost("Vehicles")]
    public async Task<ActionResult<VehicleDto>> Create(CreateVehicleDto dto)
    {
        var userId = User.GetUserId();

        if (dto.Type == VehicleType.Motorcycle && dto.Capacity > 1)
        {
            return BadRequest(new { message = "A motorcyle vehicle type cannot have more than 1 available seat." });
        }

        var vehicle = new Vehicle
        {
            UserId = dto.UserId,
            Type = dto.Type,
            PlateNumber = dto.PlateNumber,
            Model = dto.Model,
            Capacity = dto.Capacity
        };

        db.Vehicles.Add(vehicle);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(new { message = "A vehicle with this plate number already exists." });
        }

        var result = new VehicleDto(vehicle.VehicleId, vehicle.UserId, vehicle.Type, vehicle.PlateNumber, vehicle.Model, vehicle.Capacity);
        return CreatedAtAction(nameof(GetById), new { vehicleId = vehicle.VehicleId }, result);
    }

    [HttpPut("Vehicles/{vehicleId}")]
    public async Task<ActionResult<VehicleDto>> Update(Guid vehicleId, UpdateVehicleDto dto)
    {
        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        if (vehicle is null) return NotFound();

        if (vehicle.UserId != User.GetUserId())
        {
            return Forbid();
        }

        vehicle.Type = dto.Type;
        vehicle.PlateNumber = dto.PlateNumber;
        vehicle.Model = dto.Model;

        if (dto.Type == VehicleType.Motorcycle && dto.Capacity > 1)
        {
            return BadRequest(new { message = "A motorcyle vehicle type cannot have more than 1 available seats." });
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return Conflict(new { message = "A vehicle with this plate number already exists." });
        }

        return Ok(new VehicleDto(vehicle.VehicleId, vehicle.UserId, vehicle.Type, vehicle.PlateNumber, vehicle.Model, vehicle.Capacity));
    }

    [HttpDelete("Vehicles/{vehicleId}")]
    public async Task<IActionResult> Delete(Guid vehicleId)
    {
        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        if (vehicle is null) return NotFound();

        if (vehicle.UserId != User.GetUserId())
        {
            return Forbid();
        }

        var hasTrips = await db.Trips.AnyAsync(t => t.VehicleId == vehicleId);
        if (hasTrips)
        {
            return Conflict(new { message = "This vehicle has trips on record and cannot be deleted." });
        }

        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync();

        return NoContent();
    }
}