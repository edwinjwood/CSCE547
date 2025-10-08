using System;

namespace DirtBikePark.Cli.Infrastructure.Persistence.Entities;

public class BookingReservedDateRecord
{
    public Guid BookingId { get; set; }
    public DateOnly Date { get; set; }

    public BookingRecord Booking { get; set; } = null!;
}
