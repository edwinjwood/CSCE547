using System;
using System.Collections.Generic;
using DirtBikePark.Cli.Domain.Enums;

namespace DirtBikePark.Cli.Infrastructure.Persistence.Entities;

public class BookingRecord
{
    public Guid Id { get; set; }
    public Guid ParkId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public int Guests { get; set; }
    public DateOnly StartDate { get; set; }
    public int DayCount { get; set; }
    public decimal PricePerDayAmount { get; set; }
    public string PricePerDayCurrency { get; set; } = "USD";
    public BookingStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public GuestCategory GuestCategory { get; set; }

    public ParkRecord Park { get; set; } = null!;
    public ICollection<BookingReservedDateRecord> ReservedDates { get; set; } = new List<BookingReservedDateRecord>();
}
