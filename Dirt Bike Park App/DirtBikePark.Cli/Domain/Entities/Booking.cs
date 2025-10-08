using System;
using System.Collections.Generic;
using System.Linq;
using DirtBikePark.Cli.Domain.Enums;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Entities;

/// <summary>
/// Represents a reservation for a park tied to a guest.
/// </summary>
public class Booking
{
    public Guid Id { get; }
    public Guid ParkId { get; }
    public string GuestName { get; }
    public int Guests { get; }
    public DateOnly StartDate { get; }
    public int DayCount { get; }
    public BookingStatus Status { get; private set; }
    public Money PricePerDay { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? CancelledAtUtc { get; private set; }
    public IReadOnlyCollection<DateOnly> ReservedDates { get; }
    public GuestCategory GuestCategory { get; }

    public Money TotalPrice => new Money(PricePerDay.Amount * Guests * DayCount, PricePerDay.Currency);

    public Booking(
        Guid parkId,
        string guestName,
        int guests,
        DateOnly startDate,
        int dayCount,
        Money pricePerDay,
    IEnumerable<DateOnly> reservedDates,
    GuestCategory guestCategory)
    {
        if (dayCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayCount), "Bookings must be for at least one day.");
        }

        if (guests <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(guests), "Guest count must be positive.");
        }

        Id = Guid.NewGuid();
        ParkId = parkId;
        GuestName = guestName ?? throw new ArgumentNullException(nameof(guestName));
        Guests = guests;
        StartDate = startDate;
        DayCount = dayCount;
        PricePerDay = pricePerDay;
        ReservedDates = reservedDates?.ToArray() ?? throw new ArgumentNullException(nameof(reservedDates));
        Status = BookingStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        GuestCategory = guestCategory;
    }

    internal Booking(
        Guid id,
        Guid parkId,
        string guestName,
        int guests,
        DateOnly startDate,
        int dayCount,
        Money pricePerDay,
        IEnumerable<DateOnly> reservedDates,
        BookingStatus status,
        DateTime createdAtUtc,
        DateTime? cancelledAtUtc,
        GuestCategory guestCategory)
    {
        Id = id;
        ParkId = parkId;
        GuestName = guestName;
        Guests = guests;
        StartDate = startDate;
        DayCount = dayCount;
        PricePerDay = pricePerDay;
        ReservedDates = reservedDates.ToArray();
        Status = status;
        CreatedAtUtc = createdAtUtc;
        CancelledAtUtc = cancelledAtUtc;
        GuestCategory = guestCategory;
    }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
    }

    public void Cancel()
    {
        Status = BookingStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }
}
