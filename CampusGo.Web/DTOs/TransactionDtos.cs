using System.ComponentModel.DataAnnotations;
using CampusGo.Web.Models;

namespace CampusGo.Web.DTOs;

public record CreateTransactionDto(
    [Required] Guid BookingId,
    [Required, Range(0, 10000)] decimal Amount,
    [Required] TransactionType Type
);

public record UpdateTransactionStatusDto(
    [Required] TransactionStatus Status
);

public record TransactionDto(
    Guid TransactionId,
    Guid BookingId,
    decimal Amount,
    TransactionType Type,
    TransactionStatus Status,
    DateTime CreatedAt
);