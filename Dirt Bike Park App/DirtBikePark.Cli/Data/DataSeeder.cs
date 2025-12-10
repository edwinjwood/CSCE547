using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Data;

/// <summary>
/// Provides seed data for parks and bookings in the in-memory repositories.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedParksAsync(IParkRepository parkRepository, CancellationToken cancellationToken = default)
    {
        var existing = await parkRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (existing.Any())
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        static IEnumerable<DateOnly> BuildAvailability(DateOnly start)
            => Enumerable.Range(0, 14).Select(offset => start.AddDays(offset));

        var parks = new List<Park>
        {
            new(
                Guid.NewGuid(),
                "Wild Ridge Moto Ranch",
                "A challenging trail system with steep climbs and berms.",
                "Moab, UT",
                50,
                new Money(120m),
                BuildAvailability(today)),
            new(
                Guid.NewGuid(),
                "Pine Valley MX Park",
                "Family-friendly park with beginner and intermediate tracks.",
                "Greenville, SC",
                35,
                new Money(90m),
                BuildAvailability(today)),
            new(
                Guid.NewGuid(),
                "Coastal Dunes Adventure Park",
                "Ride along coastal dunes with guided tours available.",
                "Santa Cruz, CA",
                25,
                new Money(150m),
                BuildAvailability(today))
        };

        foreach (var park in parks)
        {
            await parkRepository.AddAsync(park, cancellationToken).ConfigureAwait(false);
        }
    }
    
}
