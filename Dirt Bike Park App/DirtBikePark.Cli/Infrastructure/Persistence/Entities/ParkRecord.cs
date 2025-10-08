using System;
using System.Collections.Generic;

namespace DirtBikePark.Cli.Infrastructure.Persistence.Entities;

public class ParkRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int GuestLimit { get; set; }
    public int AvailableGuestCapacity { get; set; }
    public decimal PricePerGuestPerDayAmount { get; set; }
    public string PricePerGuestPerDayCurrency { get; set; } = "USD";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public ICollection<ParkAvailabilityRecord> Availability { get; set; } = new List<ParkAvailabilityRecord>();
    public ICollection<BookingRecord> Bookings { get; set; } = new List<BookingRecord>();
}
