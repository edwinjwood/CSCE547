using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;
using DirtBikePark.Cli.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace DirtBikePark.Cli.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IParkRepository"/>.
/// </summary>
public sealed class SqliteParkRepository : IParkRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteParkRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<Park>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var parks = new List<Park>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Description, Location, GuestLimit, AvailableGuestCapacity, PricePerGuestPerDayAmount, PricePerGuestPerDayCurrency, CreatedAtUtc, LastModifiedUtc FROM parks ORDER BY Name";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var parkId = Guid.Parse(reader.GetString(0));
            var availableDates = await LoadAvailabilityAsync(connection, parkId, cancellationToken).ConfigureAwait(false);

            var park = new Park(
                parkId,
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                new Money(Convert.ToDecimal(reader.GetDouble(6), CultureInfo.InvariantCulture), reader.GetString(7)),
                availableDates,
                DateTime.Parse(reader.GetString(8), null, DateTimeStyles.RoundtripKind),
                DateTime.Parse(reader.GetString(9), null, DateTimeStyles.RoundtripKind));

            parks.Add(park);
        }

        return parks.AsReadOnly();
    }

    public async Task<Park?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Description, Location, GuestLimit, AvailableGuestCapacity, PricePerGuestPerDayAmount, PricePerGuestPerDayCurrency, CreatedAtUtc, LastModifiedUtc FROM parks WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var availability = await LoadAvailabilityAsync(connection, id, cancellationToken).ConfigureAwait(false);
        return new Park(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            new Money(Convert.ToDecimal(reader.GetDouble(6), CultureInfo.InvariantCulture), reader.GetString(7)),
            availability,
            DateTime.Parse(reader.GetString(8), null, DateTimeStyles.RoundtripKind),
            DateTime.Parse(reader.GetString(9), null, DateTimeStyles.RoundtripKind));
    }

    public async Task AddAsync(Park park, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();

        await UpsertParkAsync(connection, transaction, park, cancellationToken).ConfigureAwait(false);
        await ReplaceAvailabilityAsync(connection, transaction, park, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM parks WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task UpdateAsync(Park park, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();

        await UpsertParkAsync(connection, transaction, park, cancellationToken).ConfigureAwait(false);
        await ReplaceAvailabilityAsync(connection, transaction, park, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpsertParkAsync(SqliteConnection connection, SqliteTransaction transaction, Park park, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"INSERT INTO parks (Id, Name, Description, Location, GuestLimit, AvailableGuestCapacity, PricePerGuestPerDayAmount, PricePerGuestPerDayCurrency, CreatedAtUtc, LastModifiedUtc)
                                 VALUES ($id, $name, $description, $location, $guestLimit, $availableCapacity, $priceAmount, $priceCurrency, $createdAt, $lastModified)
                                 ON CONFLICT(Id) DO UPDATE SET
                                     Name = excluded.Name,
                                     Description = excluded.Description,
                                     Location = excluded.Location,
                                     GuestLimit = excluded.GuestLimit,
                                     AvailableGuestCapacity = excluded.AvailableGuestCapacity,
                                     PricePerGuestPerDayAmount = excluded.PricePerGuestPerDayAmount,
                                     PricePerGuestPerDayCurrency = excluded.PricePerGuestPerDayCurrency,
                                     LastModifiedUtc = excluded.LastModifiedUtc";

        command.Parameters.AddWithValue("$id", park.Id.ToString());
        command.Parameters.AddWithValue("$name", park.Name);
        command.Parameters.AddWithValue("$description", park.Description);
        command.Parameters.AddWithValue("$location", park.Location);
        command.Parameters.AddWithValue("$guestLimit", park.GuestLimit);
        command.Parameters.AddWithValue("$availableCapacity", park.AvailableGuestCapacity);
    command.Parameters.AddWithValue("$priceAmount", Convert.ToDouble(park.PricePerGuestPerDay.Amount));
        command.Parameters.AddWithValue("$priceCurrency", park.PricePerGuestPerDay.Currency);
        command.Parameters.AddWithValue("$createdAt", park.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$lastModified", park.LastModifiedUtc.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ReplaceAvailabilityAsync(SqliteConnection connection, SqliteTransaction transaction, Park park, CancellationToken cancellationToken)
    {
        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM park_availability WHERE ParkId = $id";
            deleteCommand.Parameters.AddWithValue("$id", park.Id.ToString());
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!park.AvailableDates.Any())
        {
            return;
        }

        foreach (var date in park.AvailableDates)
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = "INSERT INTO park_availability (ParkId, Date) VALUES ($id, $date)";
            insertCommand.Parameters.AddWithValue("$id", park.Id.ToString());
            insertCommand.Parameters.AddWithValue("$date", date.ToString("O"));
            await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlyCollection<DateOnly>> LoadAvailabilityAsync(SqliteConnection connection, Guid parkId, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Date FROM park_availability WHERE ParkId = $id ORDER BY Date";
        command.Parameters.AddWithValue("$id", parkId.ToString());

        var dates = new List<DateOnly>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var value = reader.GetString(0);
            dates.Add(DateOnly.ParseExact(value, "O", CultureInfo.InvariantCulture));
        }

        return dates.AsReadOnly();
    }
}
