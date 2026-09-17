namespace CampusGo.Web.Models;

public enum UserRole
{
    Rider,
    Driver,
    Both
}

public class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    // This is EF Core's way of representing a relationship from the "many" side of the ERD
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Trip> TripsAsDriver { get; set; } = new List<Trip>();
    public ICollection<Booking> BookingsAsRider { get; set; } = new List<Booking>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}