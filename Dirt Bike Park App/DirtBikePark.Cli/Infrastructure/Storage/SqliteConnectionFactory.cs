using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace DirtBikePark.Cli.Infrastructure.Storage;

/// <summary>
/// Provides configured SQLite connections and ensures the schema exists.
/// </summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string databasePath)
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

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        _connectionString = builder.ToString();
    }

    public SqliteConnection CreateConnection() => new(_connectionString);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var commands = new[]
        {
            @"CREATE TABLE IF NOT EXISTS parks (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Description TEXT NOT NULL,
                Location TEXT NOT NULL,
                GuestLimit INTEGER NOT NULL,
                AvailableGuestCapacity INTEGER NOT NULL,
                PricePerGuestPerDayAmount REAL NOT NULL,
                PricePerGuestPerDayCurrency TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                LastModifiedUtc TEXT NOT NULL
            );",
            @"CREATE TABLE IF NOT EXISTS park_availability (
                ParkId TEXT NOT NULL,
                Date TEXT NOT NULL,
                PRIMARY KEY (ParkId, Date),
                FOREIGN KEY (ParkId) REFERENCES parks(Id) ON DELETE CASCADE
            );",
            @"CREATE TABLE IF NOT EXISTS bookings (
                Id TEXT PRIMARY KEY,
                ParkId TEXT NOT NULL,
                GuestName TEXT NOT NULL,
                Guests INTEGER NOT NULL,
                StartDate TEXT NOT NULL,
                DayCount INTEGER NOT NULL,
                PricePerDayAmount REAL NOT NULL,
                PricePerDayCurrency TEXT NOT NULL,
                Status TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                CancelledAtUtc TEXT,
                GuestCategory TEXT NOT NULL,
                FOREIGN KEY (ParkId) REFERENCES parks(Id) ON DELETE CASCADE
            );",
            @"CREATE TABLE IF NOT EXISTS booking_reserved_dates (
                BookingId TEXT NOT NULL,
                Date TEXT NOT NULL,
                PRIMARY KEY (BookingId, Date),
                FOREIGN KEY (BookingId) REFERENCES bookings(Id) ON DELETE CASCADE
            );"
        };

        foreach (var commandText in commands)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
