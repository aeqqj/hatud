using CampusGo.Web.Data;
using CampusGo.Web.DTOs;
using CampusGo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusGo.Web.Controllers;

[ApiController]
[Route("api")]
public class TransactionsController(AppDbContext db) : ControllerBase
{
    private static TransactionDto ToDto(Transaction t) =>
        new(t.TransactionId, t.BookingId, t.Amount, t.Type, t.Status, t.CreatedAt);

    [HttpGet("Bookings/{bookingId}/transaction")]
    public async Task<ActionResult<TransactionDto>> GetByBooking(Guid bookingId)
    {
        var transaction = await db.Transactions.FirstOrDefaultAsync(t => t.BookingId == bookingId);
        if (transaction is null) return NotFound();

        return Ok(ToDto(transaction));
    }

    [HttpPost("Transactions")]
    public async Task<ActionResult<TransactionDto>> Create(CreateTransactionDto dto)
    {
        var booking = await db.Bookings.FindAsync(dto.BookingId);
        if (booking is null) return NotFound(new { message = "No booking found with the given BookingId." });

        var exists = await db.Transactions.AnyAsync(t => t.BookingId == dto.BookingId);
        if (exists)
        {
            return Conflict(new { message = "A transaction already exists for this booking." });
        }

        var transaction = new Transaction
        {
            BookingId = dto.BookingId,
            Amount = dto.Amount,
            Type = dto.Type,
            Status = TransactionStatus.Pending
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByBooking), new { bookingId = transaction.BookingId }, ToDto(transaction));
    }

    [HttpPatch("Transactions/{transactionId}/status")]
    public async Task<ActionResult<TransactionDto>> UpdateStatus(Guid transactionId, UpdateTransactionStatusDto dto)
    {
        var transaction = await db.Transactions.FindAsync(transactionId);
        if (transaction is null) return NotFound();

        transaction.Status = dto.Status;
        await db.SaveChangesAsync();

        return Ok(ToDto(transaction));
    }
}