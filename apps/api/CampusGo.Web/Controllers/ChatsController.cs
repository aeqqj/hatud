using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Helpers;
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
        if (!await IsRoomMember(tripId, User.GetUserId()))
        {
            return Forbid();
        }

        var room = await db.ChatRooms.FirstOrDefaultAsync(c => c.TripId == tripId);
        if (room is null) return NotFound();

        return Ok(ToRoomDto(room));
    }

    [HttpGet("ChatRooms/{chatRoomId}/messages")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessages(Guid chatRoomId)
    {
        var room = await db.ChatRooms.FindAsync(chatRoomId);
        if (room is null) return NotFound();

        if (!await IsRoomMember(room.TripId, User.GetUserId()))
        {
            return Forbid();
        }

        var messages = await db.Messages
            .Where(m => m.ChatRoomId == chatRoomId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages.Select(ToMessageDto));
    }

    [HttpPost("ChatRooms/{chatRoomId}/messages")]
    public async Task<ActionResult<MessageDto>> PostMessage(Guid chatRoomId, CreateMessageDto dto)
    {
        var senderId = User.GetUserId();

        var room = await db.ChatRooms.FindAsync(chatRoomId);
        if (room is null) return NotFound(new { message = "No chatroom found with the given ChatRoomId." });

        if (!await IsRoomMember(room.TripId, senderId))
        {
            return Forbid();
        }

        var message = new Message
        {
            ChatRoomId = chatRoomId,
            SenderId = senderId,
            Content = dto.Content,
            SentAt = DateTime.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMessages), new { chatRoomId }, ToMessageDto(message));
    }

    private async Task<bool> IsRoomMember(Guid tripId, Guid userId)
    {
        var trip = await db.Trips.FindAsync(tripId);
        if (trip is null) return false;

        if (trip.DriverId == userId) return true;

        return await db.Bookings.AnyAsync(b =>
            b.TripId == tripId && b.RiderId == userId && b.Status == BookingStatus.Confirmed);
    }
}