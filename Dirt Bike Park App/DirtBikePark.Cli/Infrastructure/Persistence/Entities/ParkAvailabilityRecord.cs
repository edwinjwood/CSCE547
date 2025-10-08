using System;

namespace DirtBikePark.Cli.Infrastructure.Persistence.Entities;

public class ParkAvailabilityRecord
{
    public Guid ParkId { get; set; }
    public DateOnly Date { get; set; }

    public ParkRecord Park { get; set; } = null!;
}
