using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api")]
public class ChatController(AppDbContext db) : ControllerBase
{
    private static ChatRoomDto ToRoomDto(ChatRoom c) => new(c.ChatRoomId, c.TripId, c.CreatedAt);
    private static MessageDto ToMessageDto(Message m) => new(m.MessageId, m.ChatRoomId, m.SenderId, m.Content, m.SentAt);

    [HttpGet("Trips/{tripId}/chatroom")]
    public async Task<ActionResult<ChatRoomDto>> GetByTrip(Guid tripId)
    {
        var room = await db.ChatRooms.FirstOrDefaultAsync(c => c.TripId == tripId);
        if (room is null) return NotFound();

        return Ok(ToRoomDto(room));
    }

    [HttpGet("ChatRooms/{chatRoomId}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(Guid chatRoomId)
    {
        var room = await db.ChatRooms.FindAsync(chatRoomId);
        if (room is null) return NotFound();

        var messages = await db.Messages
            .Where(m => m.ChatRoomId == chatRoomId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages.Select(ToMessageDto));
    }

    [HttpPost("ChatRooms/{chatRoomId}/messages")]
    public async Task<ActionResult<MessageDto>> PostMessage(Guid chatRoomId, CreateMessageDto dto)
    {
        var room = await db.ChatRooms.FindAsync(chatRoomId);
        if (room is null) return NotFound(new { message = "No chatroom found with the given ChatRoomId." });

        var trip = await db.Trips.FindAsync(room.TripId);
        if (trip is null) return NotFound(new { message = "Associated trip not found." });

        var isDriver = dto.SenderId == trip.DriverId;
        var isConfirmedRider = await db.Bookings.AnyAsync(b =>
            b.TripId == room.TripId &&
            b.RiderId == dto.SenderId &&
            b.Status == BookingStatus.Confirmed);

        if (!isDriver && !isConfirmedRider)
        {
            return Forbid();
        }

        var message = new Message
        {
            ChatRoomId = chatRoomId,
            SenderId = dto.SenderId,
            Content = dto.Content,
            SentAt = DateTime.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMessages), new { chatRoomId }, ToMessageDto(message));
    }
}