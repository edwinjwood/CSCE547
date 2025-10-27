using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Enums;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Services;

/// <summary>
/// Handles the lifecycle of bookings including creation, confirmation, and cancellation.
/// </summary>
public class BookingService
{
    private readonly IParkRepository _parkRepository;
    private readonly IBookingRepository _bookingRepository;

    public BookingService(IParkRepository parkRepository, IBookingRepository bookingRepository)
    {
        _parkRepository = parkRepository;
        _bookingRepository = bookingRepository;
    }

    /// <summary>
    /// Methods to retrieve bookings from the repository based on different criteria.
    /// </summary>
    
    //Get all bookings in the system
    public Task<IReadOnlyCollection<Booking>> GetAllBookingsAsync(CancellationToken cancellationToken = default)
        => _bookingRepository.GetAllAsync(cancellationToken);
    //Get bookings for a specific park
    public Task<IReadOnlyCollection<Booking>> GetBookingsByParkAsync(Guid parkId, CancellationToken cancellationToken = default)
        => _bookingRepository.GetByParkAsync(parkId, cancellationToken);
    //Get a specific booking by its bookingId
    public Task<Booking?> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
        => _bookingRepository.GetByIdAsync(bookingId, cancellationToken);

    //Create a new booking for a park. Used for multiple day bookings.
    public async Task<Booking?> CreateBookingAsync(
        Guid parkId,
        string guestName,
        int guests,
        int dayCount,
        CancellationToken cancellationToken = default)
    {
        //Retrieve the park from the repository via its parkId
        var park = await _parkRepository.GetByIdAsync(parkId, cancellationToken).ConfigureAwait(false);
        //Check if the park exists, has availability for the requested guests, and can reserve the requested dates
        if (park is null)
        {
            return null;
        }

        if (!park.HasAvailabilityFor(guests))
        {
            return null;
        }

        if (!park.TryReserveDates(dayCount, out var reservedDates))
        {
            return null;
        }

        // Reserve guests and update the park repository
        park.ReserveGuests(guests);
        await _parkRepository.UpdateAsync(park, cancellationToken).ConfigureAwait(false);

        var booking = new Booking(
            parkId,
            guestName,
            guests,
            reservedDates.First(),
            dayCount,
            park.PricePerGuestPerDay,
            reservedDates,
            GuestCategory.Adult);

        booking.Confirm();
        await _bookingRepository.AddAsync(booking, cancellationToken).ConfigureAwait(false);
        return booking;
    }

    //Create a new booking for a single day
    public async Task<Booking?> CreateSingleDayBookingAsync(
        Guid parkId,
        string guestName,
        GuestCategory guestCategory,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var park = await _parkRepository.GetByIdAsync(parkId, cancellationToken).ConfigureAwait(false);
        if (park is null)
        {
            return null;
        }

        if (!park.HasAvailabilityFor(1))
        {
            return null;
        }

        if (!park.TryReserveSpecificDate(date))
        {
            return null;
        }

        park.ReserveGuests(1);
        await _parkRepository.UpdateAsync(park, cancellationToken).ConfigureAwait(false);

        var pricePerDay = guestCategory switch
        {
            GuestCategory.Child => park.PricePerGuestPerDay * 0.6m,
            _ => park.PricePerGuestPerDay
        };

        var safeGuestName = string.IsNullOrWhiteSpace(guestName) ? "Guest" : guestName.Trim();
        var booking = new Booking(
            parkId,
            safeGuestName,
            1,
            date,
            1,
            pricePerDay,
            new[] { date },
            guestCategory);

        booking.Confirm();
        await _bookingRepository.AddAsync(booking, cancellationToken).ConfigureAwait(false);
        return booking;
    }

    //Cancel an existing booking
    public async Task<bool> CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false);
        if (booking is null)
        {
            return false;
        }

        var park = await _parkRepository.GetByIdAsync(booking.ParkId, cancellationToken).ConfigureAwait(false);
        if (park is null)
        {
            return false;
        }

        booking.Cancel();
        park.ReleaseGuests(booking.Guests);
        park.ReleaseDates(booking.ReservedDates);

        await _bookingRepository.UpdateAsync(booking, cancellationToken).ConfigureAwait(false);
        await _parkRepository.UpdateAsync(park, cancellationToken).ConfigureAwait(false);
        return true;
    }

    //Remove a booking from the system
    public async Task<bool> RemoveBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false);
        if (booking is null)
        {
            return false;
        }

        var park = await _parkRepository.GetByIdAsync(booking.ParkId, cancellationToken).ConfigureAwait(false);
        if (park is null)
        {
            return false;
        }

        park.ReleaseGuests(booking.Guests);
        park.ReleaseDates(booking.ReservedDates);
        await _parkRepository.UpdateAsync(park, cancellationToken).ConfigureAwait(false);
        return await _bookingRepository.RemoveAsync(bookingId, cancellationToken).ConfigureAwait(false);
    }
}
