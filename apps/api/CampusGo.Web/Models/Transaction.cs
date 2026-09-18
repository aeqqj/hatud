namespace CampusGo.Web.Models;

public enum TransactionType
{
    Fare,
    PlatformFee,
    Refund
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public class Transaction
{
    public Guid TransactionId { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}