using System;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Entities;

/// <summary>
/// Represents an item in a shopping cart referencing a booking.
/// </summary>
public class CartItem
{
    public Guid BookingId { get; }
    public string ParkName { get; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; }
    public Money Subtotal => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    public CartItem(Guid bookingId, string parkName, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        BookingId = bookingId;
        ParkName = string.IsNullOrWhiteSpace(parkName)
            ? throw new ArgumentException("Park name cannot be empty.", nameof(parkName))
            : parkName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        Quantity = quantity;
    }
}
