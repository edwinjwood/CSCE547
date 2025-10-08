using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DirtBikePark.Cli.Infrastructure.Storage;

/// <summary>
/// Provides configured EF Core contexts backed by SQLite and ensures the schema exists.
/// </summary>
public sealed class DirtBikeParkDbContextFactory
{
    private readonly DbContextOptions<DirtBikeParkDbContext> _options;

    public DirtBikeParkDbContextFactory(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path cannot be empty.", nameof(databasePath));
        }

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _options = new DbContextOptionsBuilder<DirtBikeParkDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
    }

    public DirtBikeParkDbContext CreateDbContext() => new(_options);

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }
}
