using System;
using System.Collections.Generic;
using System.Linq;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Entities;

/// <summary>
/// Represents a user's shopping cart containing bookings ready for checkout.
/// </summary>
public class Cart
{
    private readonly Dictionary<Guid, CartItem> _items = new();
    private readonly Stack<IReadOnlyDictionary<Guid, CartItem>> _history = new();

    public Guid Id { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastUpdatedUtc { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.Values.ToList().AsReadOnly();

    public Cart(Guid? id = null)
    {
        Id = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
        CreatedAtUtc = DateTime.UtcNow;
        LastUpdatedUtc = CreatedAtUtc;
    }

    internal Cart(Guid id, DateTime createdAtUtc, DateTime lastUpdatedUtc, IEnumerable<CartItem> items)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Cart id cannot be empty.", nameof(id));
        }

        Id = id;
        CreatedAtUtc = createdAtUtc;
        LastUpdatedUtc = lastUpdatedUtc;

        foreach (var item in items)
        {
            _items[item.BookingId] = item;
        }
    }

    public void AddOrUpdateItem(CartItem item)
    {
        Snapshot();
        _items[item.BookingId] = item;
        Touch();
    }

    public bool RemoveItem(Guid bookingId)
    {
        if (_items.Remove(bookingId))
        {
            Touch();
            return true;
        }

        return false;
    }

    public bool TryGetItem(Guid bookingId, out CartItem? item)
    {
        return _items.TryGetValue(bookingId, out item);
    }

    public void Clear()
    {
        Snapshot();
        _items.Clear();
        Touch();
    }

    public bool Undo()
    {
        if (_history.TryPop(out var previousState))
        {
            _items.Clear();
            foreach (var entry in previousState)
            {
                _items[entry.Key] = entry.Value;
            }

            Touch();
            return true;
        }

        return false;
    }

    public Money Total(Func<IEnumerable<CartItem>, Money> aggregator) => aggregator(_items.Values);

    private void Snapshot()
    {
        var clone = _items.ToDictionary(kvp => kvp.Key, kvp => new CartItem(kvp.Value.BookingId, kvp.Value.ParkName, kvp.Value.Quantity, kvp.Value.UnitPrice));
        _history.Push(clone);
    }

    private void Touch()
    {
        LastUpdatedUtc = DateTime.UtcNow;
    }
}
