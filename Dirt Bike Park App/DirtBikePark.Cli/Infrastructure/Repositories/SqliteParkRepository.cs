using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;
using DirtBikePark.Cli.Infrastructure.Persistence.Entities;
using DirtBikePark.Cli.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace DirtBikePark.Cli.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IParkRepository"/>.
/// </summary>
public sealed class SqliteParkRepository : IParkRepository
{
    private readonly DirtBikeParkDbContextFactory _contextFactory;

    public SqliteParkRepository(DirtBikeParkDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyCollection<Park>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var records = await context.Parks
            .AsNoTracking()
            .Include(p => p.Availability)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(MapToDomain).ToList().AsReadOnly();
    }

    public async Task<Park?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var record = await context.Parks
            .AsNoTracking()
            .Include(p => p.Availability)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : MapToDomain(record);
    }

    public async Task AddAsync(Park park, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var record = MapToRecord(park);
        context.Parks.Add(record);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var record = await context.Parks.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return false;
        }

        context.Parks.Remove(record);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task UpdateAsync(Park park, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existing = await context.Parks
            .Include(p => p.Availability)
            .SingleOrDefaultAsync(p => p.Id == park.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            context.Parks.Add(MapToRecord(park));
        }
        else
        {
            existing.Name = park.Name;
            existing.Description = park.Description;
            existing.Location = park.Location;
            existing.GuestLimit = park.GuestLimit;
            existing.AvailableGuestCapacity = park.AvailableGuestCapacity;
            existing.PricePerGuestPerDayAmount = park.PricePerGuestPerDay.Amount;
            existing.PricePerGuestPerDayCurrency = park.PricePerGuestPerDay.Currency;
            existing.LastModifiedUtc = park.LastModifiedUtc;

            existing.Availability.Clear();
            foreach (var date in park.AvailableDates.OrderBy(d => d))
            {
                existing.Availability.Add(new ParkAvailabilityRecord
                {
                    ParkId = existing.Id,
                    Date = date
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Park MapToDomain(ParkRecord record)
    {
        return new Park(
            record.Id,
            record.Name,
            record.Description,
            record.Location,
            record.GuestLimit,
            record.AvailableGuestCapacity,
            new Money(record.PricePerGuestPerDayAmount, record.PricePerGuestPerDayCurrency),
            record.Availability.Select(a => a.Date),
            record.CreatedAtUtc,
            record.LastModifiedUtc);
    }

    private static ParkRecord MapToRecord(Park park)
    {
        var record = new ParkRecord
        {
            Id = park.Id,
            Name = park.Name,
            Description = park.Description,
            Location = park.Location,
            GuestLimit = park.GuestLimit,
            AvailableGuestCapacity = park.AvailableGuestCapacity,
            PricePerGuestPerDayAmount = park.PricePerGuestPerDay.Amount,
            PricePerGuestPerDayCurrency = park.PricePerGuestPerDay.Currency,
            CreatedAtUtc = park.CreatedAtUtc,
            LastModifiedUtc = park.LastModifiedUtc
        };

        foreach (var date in park.AvailableDates.OrderBy(d => d))
        {
            record.Availability.Add(new ParkAvailabilityRecord
            {
                ParkId = park.Id,
                Date = date
            });
        }

        return record;
    }
}
