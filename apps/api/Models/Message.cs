namespace CampusGo.Web.Models;

public class Message
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid ChatRoomId { get; set; }
    public ChatRoom? ChatRoom { get; set; }
    public Guid SenderId { get; set; }
    public User? Sender { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}