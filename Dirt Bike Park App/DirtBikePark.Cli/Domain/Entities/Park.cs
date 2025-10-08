using System;
using System.Collections.Generic;
using System.Linq;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Entities;

/// <summary>
/// Represents a dirt bike park that can be booked by guests.
/// </summary>
public class Park
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Location { get; private set; }
    public int GuestLimit { get; private set; }
    public int AvailableGuestCapacity { get; private set; }
    public Money PricePerGuestPerDay { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime LastModifiedUtc { get; private set; }

    private readonly SortedSet<DateOnly> _availableDates;

    public IReadOnlyCollection<DateOnly> AvailableDates => _availableDates.OrderBy(d => d).ToList().AsReadOnly();

    public Park(
        Guid id,
        string name,
        string description,
        string location,
        int guestLimit,
        Money pricePerGuestPerDay,
        IEnumerable<DateOnly> initialAvailability)
    {
        if (guestLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(guestLimit), "Guest limit must be greater than zero.");
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = ValidateRequired(name, nameof(name));
        Description = ValidateRequired(description, nameof(description));
        Location = ValidateRequired(location, nameof(location));
        GuestLimit = guestLimit;
        AvailableGuestCapacity = guestLimit;
        PricePerGuestPerDay = pricePerGuestPerDay;
    CreatedAtUtc = DateTime.UtcNow;
    LastModifiedUtc = CreatedAtUtc;
    _availableDates = new SortedSet<DateOnly>(initialAvailability.Distinct());
    }

    internal Park(
        Guid id,
        string name,
        string description,
        string location,
        int guestLimit,
        int availableGuestCapacity,
        Money pricePerGuestPerDay,
        IEnumerable<DateOnly> availableDates,
        DateTime createdAtUtc,
        DateTime lastModifiedUtc)
    {
        Id = id;
        Name = name;
        Description = description;
        Location = location;
        GuestLimit = guestLimit;
        AvailableGuestCapacity = availableGuestCapacity;
        PricePerGuestPerDay = pricePerGuestPerDay;
        CreatedAtUtc = createdAtUtc;
    LastModifiedUtc = lastModifiedUtc;
    _availableDates = new SortedSet<DateOnly>(availableDates);
    }

    public bool HasAvailabilityFor(int requestedGuests)
    {
        return requestedGuests > 0 && AvailableGuestCapacity >= requestedGuests;
    }

    public void ReserveGuests(int guests)
    {
        EnsurePositive(guests, nameof(guests));
        if (!HasAvailabilityFor(guests))
        {
            throw new InvalidOperationException("Not enough capacity available for the requested number of guests.");
        }

        AvailableGuestCapacity -= guests;
        LastModifiedUtc = DateTime.UtcNow;
    }

    public void ReleaseGuests(int guests)
    {
        EnsurePositive(guests, nameof(guests));
        var newCapacity = AvailableGuestCapacity + guests;
        AvailableGuestCapacity = Math.Min(newCapacity, GuestLimit);
        LastModifiedUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description, string location)
    {
        Name = ValidateRequired(name, nameof(name));
        Description = ValidateRequired(description, nameof(description));
        Location = ValidateRequired(location, nameof(location));
        LastModifiedUtc = DateTime.UtcNow;
    }

    public void UpdateGuestLimit(int newGuestLimit)
    {
        EnsurePositive(newGuestLimit, nameof(newGuestLimit));
        if (newGuestLimit < GuestLimit - AvailableGuestCapacity)
        {
            throw new InvalidOperationException("Cannot reduce guest limit below current active bookings.");
        }

        var guestsCurrentlyBooked = GuestLimit - AvailableGuestCapacity;
        GuestLimit = newGuestLimit;
        AvailableGuestCapacity = GuestLimit - guestsCurrentlyBooked;
        LastModifiedUtc = DateTime.UtcNow;
    }

    public void UpdatePrice(Money newPrice)
    {
        PricePerGuestPerDay = newPrice;
        LastModifiedUtc = DateTime.UtcNow;
    }

    public bool TryReserveDates(int dayCount, out IReadOnlyCollection<DateOnly> reservedDates)
    {
        EnsurePositive(dayCount, nameof(dayCount));
        var ordered = _availableDates.OrderBy(d => d).Take(dayCount).ToList();
        if (ordered.Count < dayCount)
        {
            reservedDates = Array.Empty<DateOnly>();
            return false;
        }

        foreach (var date in ordered)
        {
            _availableDates.Remove(date);
        }

        reservedDates = ordered.AsReadOnly();
        LastModifiedUtc = DateTime.UtcNow;
        return true;
    }

    public bool TryReserveSpecificDate(DateOnly date)
    {
        if (!_availableDates.Remove(date))
        {
            return false;
        }

        LastModifiedUtc = DateTime.UtcNow;
        return true;
    }

    public void ReleaseDates(IEnumerable<DateOnly> dates)
    {
        foreach (var date in dates)
        {
            ReleaseDate(date);
        }

        LastModifiedUtc = DateTime.UtcNow;
    }

    public void ReleaseDate(DateOnly date)
    {
        _availableDates.Add(date);

        LastModifiedUtc = DateTime.UtcNow;
    }

    private static string ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} cannot be null or whitespace.", fieldName);
        }

        return value.Trim();
    }

    private static void EnsurePositive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(fieldName, "Value must be positive.");
        }
    }
}
