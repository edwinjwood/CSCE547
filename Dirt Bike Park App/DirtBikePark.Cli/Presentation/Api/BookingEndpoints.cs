using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Enums;
using DirtBikePark.Cli.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DirtBikePark.Cli.Presentation.Api;

/// <summary>
/// Provides REST endpoints for bookings.
/// </summary>
public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var bookings = endpoints.MapGroup("/api/bookings");

        bookings.MapGet("/", async (BookingService service, CancellationToken ct) =>
            {
                var results = await service.GetAllBookingsAsync(ct).ConfigureAwait(false);
                return Results.Ok(results.Select(BookingResponse.FromDomain));
            })
            .WithName("ListBookings")
            .WithSummary("Retrieve all bookings")
            .WithDescription("Returns every booking currently persisted.");

        bookings.MapGet("/{bookingId:guid}", async (Guid bookingId, BookingService service, CancellationToken ct) =>
            {
                var booking = await service.GetBookingAsync(bookingId, ct).ConfigureAwait(false);
                return booking is null
                    ? Results.NotFound()
                    : Results.Ok(BookingResponse.FromDomain(booking));
            })
            .WithName("GetBookingById")
            .WithSummary("Retrieve a booking by identifier")
            .WithDescription("Returns a single booking when the supplied identifier exists.");

        bookings.MapDelete("/{bookingId:guid}", async (Guid bookingId, BookingService service, CancellationToken ct) =>
            {
                var removed = await service.RemoveBookingAsync(bookingId, ct).ConfigureAwait(false);
                return removed ? Results.NoContent() : Results.NotFound();
            })
            .WithName("RemoveBooking")
            .WithSummary("Remove a booking")
            .WithDescription("Deletes a booking and releases its reserved capacity.");

        var parkBookings = endpoints.MapGroup("/api/parks/{parkId:guid}/bookings");

        parkBookings.MapGet("/", async (Guid parkId, BookingService service, CancellationToken ct) =>
            {
                var results = await service.GetBookingsByParkAsync(parkId, ct).ConfigureAwait(false);
                return Results.Ok(results.Select(BookingResponse.FromDomain));
            })
            .WithName("GetBookingsForPark")
            .WithSummary("Retrieve bookings for a park")
            .WithDescription("Returns every booking associated with the specified park.");

        parkBookings.MapPost("/", async (Guid parkId, CreateBookingRequest request, BookingService service, CancellationToken ct) =>
            {
                if (!request.TryValidate(out var guestCategory, out var date, out var errors))
                {
                    return Results.ValidationProblem(errors);
                }

                Booking? booking;
                if (request.DayCount > 1)
                {
                    booking = await service.CreateBookingAsync(parkId, request.GuestName, request.Guests, request.DayCount, ct).ConfigureAwait(false);
                }
                else
                {
                    booking = await service.CreateSingleDayBookingAsync(parkId, request.GuestName, guestCategory, date, ct).ConfigureAwait(false);
                }

                return booking is null
                    ? Results.BadRequest(new { message = "Unable to create booking for the supplied criteria." })
                    : Results.Created($"/api/bookings/{booking.Id}", BookingResponse.FromDomain(booking));
            })
            .WithName("CreateBooking")
            .WithSummary("Create a booking for a park")
            .WithDescription("Creates a booking against the supplied park identifier.");
    }

    private sealed record BookingResponse(
        Guid Id,
        Guid ParkId,
        string GuestName,
        int Guests,
        string GuestCategory,
        string Status,
        string StartDate,
        int DayCount,
        decimal PricePerDay,
        decimal TotalPrice,
        string Currency,
        IReadOnlyCollection<string> ReservedDates,
        DateTime CreatedAtUtc,
        DateTime? CancelledAtUtc)
    {
        public static BookingResponse FromDomain(Booking booking)
        {
            var reservedDates = booking.ReservedDates
                .Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .ToList()
                .AsReadOnly();

            return new BookingResponse(
                booking.Id,
                booking.ParkId,
                booking.GuestName,
                booking.Guests,
                booking.GuestCategory.ToString(),
                booking.Status.ToString(),
                booking.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                booking.DayCount,
                booking.PricePerDay.Amount,
                booking.TotalPrice.Amount,
                booking.PricePerDay.Currency,
                reservedDates,
                booking.CreatedAtUtc,
                booking.CancelledAtUtc);
        }
    }

    private sealed class CreateBookingRequest
    {
        public string GuestName { get; init; } = "Guest";
        public int Guests { get; init; } = 1;
        public int DayCount { get; init; } = 1;
    public string GuestCategoryName { get; init; } = nameof(GuestCategory.Adult);
    public string? Date { get; init; }

        public bool TryValidate(out GuestCategory category, out DateOnly date, out Dictionary<string, string[]> errors)
        {
            errors = new Dictionary<string, string[]>();
            category = GuestCategory.Adult;
            date = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            if (Guests <= 0)
            {
                errors["Guests"] = new[] { "Guests must be greater than zero." };
            }

            if (DayCount <= 0)
            {
                errors["DayCount"] = new[] { "Day count must be at least one." };
            }

            if (Enum.TryParse<GuestCategory>(GuestCategoryName, true, out var parsedCategory))
            {
                category = parsedCategory;
            }
            else
            {
                errors["GuestCategory"] = new[] { "Guest category must be Adult or Child." };
            }

            if (!string.IsNullOrWhiteSpace(Date))
            {
                if (!DateOnly.TryParse(Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    errors["Date"] = new[] { "Date must be in yyyy-MM-dd format." };
                }
            }

            return errors.Count == 0;
        }
    }
}
