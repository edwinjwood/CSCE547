using System;

namespace DirtBikePark.Cli.Infrastructure.Persistence.Entities;

public class CartItemRecord
{
    public Guid CartId { get; set; }
    public Guid BookingId { get; set; }
    public string ParkName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceAmount { get; set; }
    public string UnitPriceCurrency { get; set; } = "USD";

    public CartRecord Cart { get; set; } = null!;
}
