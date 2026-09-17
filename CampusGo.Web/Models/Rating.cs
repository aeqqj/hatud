namespace CampusGo.Web.Models;

public class Rating
{
    public Guid RatingId { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid RaterId { get; set; }
    public Guid RateeId { get; set; }
    public Booking? Booking { get; set; }
    public User? Rater { get; set; }
    public User? Ratee { get; set; }
    public int Score { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}