namespace CampusGo.Web.Models;

public enum VehicleType
{
    Car,
    Motorcycle
}

public class Vehicle
{
    public Guid VehicleId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? Owner { get; set; }
    public VehicleType Type { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Capacity { get; set; }
}