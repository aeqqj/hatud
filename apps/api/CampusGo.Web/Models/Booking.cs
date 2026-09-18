namespace CampusGo.Web.Models;

using NetTopologySuite.Geometries;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected,
    Cancelled,
    Completed,
    NoShow
}

public class Booking
{
    public Guid BookingId { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Guid RiderId { get; set; }
    public Trip? Trip { get; set; }
    public User? Rider { get; set; }
    public Point PickupPoint { get; set; } = default!;
    public string PickupLabel { get; set; } = string.Empty;
    public Point DropoffPoint { get; set; } = default!;
    public string DropoffLabel { get; set; } = string.Empty;
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal Fare { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}