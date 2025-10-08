using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Enums;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;
using DirtBikePark.Cli.Infrastructure.Persistence.Entities;
using DirtBikePark.Cli.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace DirtBikePark.Cli.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IBookingRepository"/>.
/// </summary>
public sealed class SqliteBookingRepository : IBookingRepository
{
    private readonly DirtBikeParkDbContextFactory _contextFactory;

    public SqliteBookingRepository(DirtBikeParkDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyCollection<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var records = await context.Bookings
            .AsNoTracking()
            .Include(b => b.ReservedDates)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(MapToDomain).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Booking>> GetByParkAsync(Guid parkId, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var records = await context.Bookings
            .AsNoTracking()
            .Include(b => b.ReservedDates)
            .Where(b => b.ParkId == parkId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(MapToDomain).ToList().AsReadOnly();
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var record = await context.Bookings
            .AsNoTracking()
            .Include(b => b.ReservedDates)
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : MapToDomain(record);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var record = MapToRecord(booking);
        context.Bookings.Add(record);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existing = await context.Bookings
            .Include(b => b.ReservedDates)
            .SingleOrDefaultAsync(b => b.Id == booking.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return false;
        }

        existing.GuestName = booking.GuestName;
        existing.Guests = booking.Guests;
        existing.StartDate = booking.StartDate;
        existing.DayCount = booking.DayCount;
        existing.PricePerDayAmount = booking.PricePerDay.Amount;
        existing.PricePerDayCurrency = booking.PricePerDay.Currency;
        existing.Status = booking.Status;
        existing.CancelledAtUtc = booking.CancelledAtUtc;
        existing.GuestCategory = booking.GuestCategory;

        existing.ReservedDates.Clear();
        foreach (var date in booking.ReservedDates.OrderBy(d => d))
        {
            existing.ReservedDates.Add(new BookingReservedDateRecord
            {
                BookingId = booking.Id,
                Date = date
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existing = await context.Bookings.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        context.Bookings.Remove(existing);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static Booking MapToDomain(BookingRecord record)
    {
        return new Booking(
            record.Id,
            record.ParkId,
            record.GuestName,
            record.Guests,
            record.StartDate,
            record.DayCount,
            new Money(record.PricePerDayAmount, record.PricePerDayCurrency),
            record.ReservedDates.Select(rd => rd.Date),
            record.Status,
            record.CreatedAtUtc,
            record.CancelledAtUtc,
            record.GuestCategory);
    }

    private static BookingRecord MapToRecord(Booking booking)
    {
        var record = new BookingRecord
        {
            Id = booking.Id,
            ParkId = booking.ParkId,
            GuestName = booking.GuestName,
            Guests = booking.Guests,
            StartDate = booking.StartDate,
            DayCount = booking.DayCount,
            PricePerDayAmount = booking.PricePerDay.Amount,
            PricePerDayCurrency = booking.PricePerDay.Currency,
            Status = booking.Status,
            CreatedAtUtc = booking.CreatedAtUtc,
            CancelledAtUtc = booking.CancelledAtUtc,
            GuestCategory = booking.GuestCategory
        };

        foreach (var date in booking.ReservedDates.OrderBy(d => d))
        {
            record.ReservedDates.Add(new BookingReservedDateRecord
            {
                BookingId = booking.Id,
                Date = date
            });
        }

        return record;
    }
}
