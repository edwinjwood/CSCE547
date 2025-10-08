using System;
using System.Collections.Generic;

namespace DirtBikePark.Cli.Infrastructure.Persistence.Entities;

public class CartRecord
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }

    public ICollection<CartItemRecord> Items { get; set; } = new List<CartItemRecord>();
}
