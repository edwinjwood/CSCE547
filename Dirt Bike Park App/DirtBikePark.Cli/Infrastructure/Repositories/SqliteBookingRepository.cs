using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Enums;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;
using DirtBikePark.Cli.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace DirtBikePark.Cli.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="IBookingRepository"/>.
/// </summary>
public sealed class SqliteBookingRepository : IBookingRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteBookingRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, ParkId, GuestName, Guests, StartDate, DayCount, PricePerDayAmount, PricePerDayCurrency, Status, CreatedAtUtc, CancelledAtUtc, GuestCategory FROM bookings ORDER BY CreatedAtUtc DESC";

        var rows = new List<BookingRow>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(ReadBookingRow(reader));
            }
        }

        var bookings = new List<Booking>(rows.Count);
        foreach (var row in rows)
        {
            var reservedDates = await LoadReservedDatesAsync(connection, row.Id, cancellationToken).ConfigureAwait(false);
            bookings.Add(MapBooking(row, reservedDates));
        }

        return bookings.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Booking>> GetByParkAsync(Guid parkId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, ParkId, GuestName, Guests, StartDate, DayCount, PricePerDayAmount, PricePerDayCurrency, Status, CreatedAtUtc, CancelledAtUtc, GuestCategory FROM bookings WHERE ParkId = $parkId ORDER BY CreatedAtUtc DESC";
        command.Parameters.AddWithValue("$parkId", parkId.ToString());

        var rows = new List<BookingRow>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(ReadBookingRow(reader));
            }
        }

        var bookings = new List<Booking>(rows.Count);
        foreach (var row in rows)
        {
            var reservedDates = await LoadReservedDatesAsync(connection, row.Id, cancellationToken).ConfigureAwait(false);
            bookings.Add(MapBooking(row, reservedDates));
        }

        return bookings.AsReadOnly();
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, ParkId, GuestName, Guests, StartDate, DayCount, PricePerDayAmount, PricePerDayCurrency, Status, CreatedAtUtc, CancelledAtUtc, GuestCategory FROM bookings WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        BookingRow? row = null;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                row = ReadBookingRow(reader);
            }
        }

        if (row is not BookingRow bookingRow)
        {
            return null;
        }

        var reservedDates = await LoadReservedDatesAsync(connection, bookingRow.Id, cancellationToken).ConfigureAwait(false);
        return MapBooking(bookingRow, reservedDates);
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO bookings (Id, ParkId, GuestName, Guests, StartDate, DayCount, PricePerDayAmount, PricePerDayCurrency, Status, CreatedAtUtc, CancelledAtUtc, GuestCategory)
                                      VALUES ($id, $parkId, $guestName, $guests, $startDate, $dayCount, $priceAmount, $priceCurrency, $status, $createdAt, $cancelledAt, $guestCategory)";

            command.Parameters.AddWithValue("$id", booking.Id.ToString());
            command.Parameters.AddWithValue("$parkId", booking.ParkId.ToString());
            command.Parameters.AddWithValue("$guestName", booking.GuestName);
            command.Parameters.AddWithValue("$guests", booking.Guests);
            command.Parameters.AddWithValue("$startDate", booking.StartDate.ToString("O"));
            command.Parameters.AddWithValue("$dayCount", booking.DayCount);
            command.Parameters.AddWithValue("$priceAmount", Convert.ToDouble(booking.PricePerDay.Amount));
            command.Parameters.AddWithValue("$priceCurrency", booking.PricePerDay.Currency);
            command.Parameters.AddWithValue("$status", booking.Status.ToString());
            command.Parameters.AddWithValue("$createdAt", booking.CreatedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$cancelledAt", booking.CancelledAtUtc?.ToString("O"));
            command.Parameters.AddWithValue("$guestCategory", booking.GuestCategory.ToString());

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await ReplaceReservedDatesAsync(connection, transaction, booking, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = @"UPDATE bookings SET
                                    GuestName = $guestName,
                                    Guests = $guests,
                                    StartDate = $startDate,
                                    DayCount = $dayCount,
                                    PricePerDayAmount = $priceAmount,
                                    PricePerDayCurrency = $priceCurrency,
                                    Status = $status,
                                    CancelledAtUtc = $cancelledAt,
                                    GuestCategory = $guestCategory
                                WHERE Id = $id";

        command.Parameters.AddWithValue("$guestName", booking.GuestName);
        command.Parameters.AddWithValue("$guests", booking.Guests);
        command.Parameters.AddWithValue("$startDate", booking.StartDate.ToString("O"));
        command.Parameters.AddWithValue("$dayCount", booking.DayCount);
    command.Parameters.AddWithValue("$priceAmount", Convert.ToDouble(booking.PricePerDay.Amount));
        command.Parameters.AddWithValue("$priceCurrency", booking.PricePerDay.Currency);
        command.Parameters.AddWithValue("$status", booking.Status.ToString());
        command.Parameters.AddWithValue("$cancelledAt", booking.CancelledAtUtc?.ToString("O"));
        command.Parameters.AddWithValue("$guestCategory", booking.GuestCategory.ToString());
        command.Parameters.AddWithValue("$id", booking.Id.ToString());

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM bookings WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rows > 0;
    }

    private static async Task ReplaceReservedDatesAsync(SqliteConnection connection, SqliteTransaction transaction, Booking booking, CancellationToken cancellationToken)
    {
        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM booking_reserved_dates WHERE BookingId = $id";
            deleteCommand.Parameters.AddWithValue("$id", booking.Id.ToString());
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var date in booking.ReservedDates)
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = "INSERT INTO booking_reserved_dates (BookingId, Date) VALUES ($id, $date)";
            insertCommand.Parameters.AddWithValue("$id", booking.Id.ToString());
            insertCommand.Parameters.AddWithValue("$date", date.ToString("O"));
            await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static BookingRow ReadBookingRow(SqliteDataReader reader)
    {
        return new BookingRow(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.GetInt32(3),
            DateOnly.ParseExact(reader.GetString(4), "O", CultureInfo.InvariantCulture),
            reader.GetInt32(5),
            new Money(Convert.ToDecimal(reader.GetDouble(6)), reader.GetString(7)),
            Enum.Parse<BookingStatus>(reader.GetString(8)),
            DateTime.Parse(reader.GetString(9), null, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10), null, DateTimeStyles.RoundtripKind),
            Enum.Parse<GuestCategory>(reader.GetString(11)));
    }

    private static Booking MapBooking(BookingRow row, IReadOnlyCollection<DateOnly> reservedDates)
    {
        return new Booking(
            row.Id,
            row.ParkId,
            row.GuestName,
            row.Guests,
            row.StartDate,
            row.DayCount,
            row.PricePerDay,
            reservedDates,
            row.Status,
            row.CreatedAtUtc,
            row.CancelledAtUtc,
            row.GuestCategory);
    }

    private static async Task<IReadOnlyCollection<DateOnly>> LoadReservedDatesAsync(SqliteConnection connection, Guid bookingId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Date FROM booking_reserved_dates WHERE BookingId = $id ORDER BY Date";
        command.Parameters.AddWithValue("$id", bookingId.ToString());

        var dates = new List<DateOnly>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                dates.Add(DateOnly.ParseExact(reader.GetString(0), "O", CultureInfo.InvariantCulture));
            }
        }

        return dates.AsReadOnly();
    }

    private sealed record BookingRow(
        Guid Id,
        Guid ParkId,
        string GuestName,
        int Guests,
        DateOnly StartDate,
        int DayCount,
        Money PricePerDay,
        BookingStatus Status,
        DateTime CreatedAtUtc,
        DateTime? CancelledAtUtc,
        GuestCategory GuestCategory);
}
