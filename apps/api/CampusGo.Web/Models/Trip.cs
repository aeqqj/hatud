namespace CampusGo.Web.Models;

public enum TripStatus
{
    Open,
    Full,
    InProgress,
    Completed,
    Cancelled
}

public class Trip
{
    public Guid TripId { get; set; } = Guid.NewGuid();
    public Guid DriverId { get; set; }
    public Guid VehicleId { get; set; }
    public User? Driver { get; set; }
    public Vehicle? Vehicle { get; set; }
    public ChatRoom? ChatRoom { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; } // specific date + time, not just TimeOnly
    public decimal Fare { get; set; }
    public int AvailableSeats { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Open;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}