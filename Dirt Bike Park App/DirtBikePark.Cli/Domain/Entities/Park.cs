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
        //Check to ensure guest limit is positive
        if (guestLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(guestLimit), "Guest limit must be greater than zero.");
        }
        //Assign Guid if empty
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        //Validate required fields to ensure they are not null or whitespace
        Name = ValidateRequired(name, nameof(name));
        Description = ValidateRequired(description, nameof(description));
        Location = ValidateRequired(location, nameof(location));
        //Set variables
        GuestLimit = guestLimit;
        AvailableGuestCapacity = guestLimit;
        PricePerGuestPerDay = pricePerGuestPerDay;
        //Set timestamps
        CreatedAtUtc = DateTime.UtcNow;
        LastModifiedUtc = CreatedAtUtc;
        //Initialize available dates
        _availableDates = new SortedSet<DateOnly>(initialAvailability.Distinct());
    }

    //Internal constructor for park
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

    //Return boolean indicating if park capacity is available for requested guests. Also checks that requested guests is positive.
    public bool HasAvailabilityFor(int requestedGuests)
    {
        return requestedGuests > 0 && AvailableGuestCapacity >= requestedGuests;
    }

    //Function to reserve guests at the selected park and update capacity and modified timestamp
    public void ReserveGuests(int guests)
    {
        //Ensure requested guests is positive
        EnsurePositive(guests, nameof(guests));
        //Check if the park has enough capacity for the requested number of guests
        if (!HasAvailabilityFor(guests))
        {
            throw new InvalidOperationException("Not enough capacity available for the requested number of guests.");
        }
        //Subtract the reserved guests from the available capacity and update the last modified timestamp
        AvailableGuestCapacity -= guests;
        LastModifiedUtc = DateTime.UtcNow;
    }

    //Function to free up guest capacity at the park and update modified timestamp
    public void ReleaseGuests(int guests)
    {
        EnsurePositive(guests, nameof(guests));
        var newCapacity = AvailableGuestCapacity + guests;
        AvailableGuestCapacity = Math.Min(newCapacity, GuestLimit);
        LastModifiedUtc = DateTime.UtcNow;
    }

    //Function to update park details. Ensures all parameters are valid and update modified timestamp
    public void UpdateDetails(string name, string description, string location)
    {
        Name = ValidateRequired(name, nameof(name));
        Description = ValidateRequired(description, nameof(description));
        Location = ValidateRequired(location, nameof(location));
        LastModifiedUtc = DateTime.UtcNow;
    }

    //Function to update the guest limit of the park.
    public void UpdateGuestLimit(int newGuestLimit)
    {
        EnsurePositive(newGuestLimit, nameof(newGuestLimit));
        //Ensure that the new guest limit is not less than the number of currently booked guests
        if (newGuestLimit < GuestLimit - AvailableGuestCapacity)
        {
            throw new InvalidOperationException("Cannot reduce guest limit below current active bookings.");
        }

        var guestsCurrentlyBooked = GuestLimit - AvailableGuestCapacity;
        //Update guest limit and available capacity accordingly
        GuestLimit = newGuestLimit;
        AvailableGuestCapacity = GuestLimit - guestsCurrentlyBooked;
        LastModifiedUtc = DateTime.UtcNow;
    }

    //Function to update the price per guest and update the timestamp
    public void UpdatePrice(Money newPrice)
    {
        PricePerGuestPerDay = newPrice;
        LastModifiedUtc = DateTime.UtcNow;
    }

    //FFunction that attempts to reserve dates for a given number of days
    public bool TryReserveDates(int dayCount, out IReadOnlyCollection<DateOnly> reservedDates)
    {
        //Ensure that the requested number of days is positive
        EnsurePositive(dayCount, nameof(dayCount));
        //Sort the dates in ascending order. Take the earliest dayCount amount of dates
        var ordered = _availableDates.OrderBy(d => d).Take(dayCount).ToList();
        //If there are not enough available dates, return false
        if (ordered.Count < dayCount)
        {
            reservedDates = Array.Empty<DateOnly>();
            return false;
        }

        //Reserve the dates by removing them from the available dates set
        foreach (var date in ordered)
        {
            _availableDates.Remove(date);
        }

        //Set the reserved dates and update the modified timestamp
        reservedDates = ordered.AsReadOnly();
        LastModifiedUtc = DateTime.UtcNow;
        return true;
    }

    //Function that attempts to reserve a specific date
    public bool TryReserveSpecificDate(DateOnly date)
    {
        //If the date cannot be removed, it means it was not available. Return false
        if (!_availableDates.Remove(date))
        {
            return false;
        }
        //Update the modified timestamp and return true
        LastModifiedUtc = DateTime.UtcNow;
        return true;
    }

    //Function to release multiple dates back to availability
    public void ReleaseDates(IEnumerable<DateOnly> dates)
    {
        foreach (var date in dates)
        {
            ReleaseDate(date);
        }

        LastModifiedUtc = DateTime.UtcNow;
    }

    //Function to release a single date back to availability
    public void ReleaseDate(DateOnly date)
    {
        _availableDates.Add(date);

        LastModifiedUtc = DateTime.UtcNow;
    }

    //Helper function to make sure required string fields are not null or whitespace
    private static string ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} cannot be null or whitespace.", fieldName);
        }

        return value.Trim();
    }
    
    //Helper function to ensure integer values are positive
    private static void EnsurePositive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(fieldName, "Value must be positive.");
        }
    }
}
