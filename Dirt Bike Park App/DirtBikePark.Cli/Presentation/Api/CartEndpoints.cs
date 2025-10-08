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
/// Provides REST endpoints for cart management.
/// </summary>
public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var carts = endpoints.MapGroup("/api/carts");

        carts.MapGet("/", async (Guid? id, CartService cartService, CancellationToken ct) =>
            {
                var cart = await cartService.GetOrCreateCartAsync(id, ct).ConfigureAwait(false);
                var totals = await cartService.CalculateTotalsAsync(cart.Id, ct).ConfigureAwait(false);
                return Results.Ok(CartResponse.FromDomain(cart, totals));
            })
            .WithName("GetCart")
            .WithSummary("Retrieve or create a cart")
            .WithDescription("Returns the specified cart when the id is provided, otherwise creates a new cart.");

        carts.MapPost("/{cartId:guid}/items", async (Guid cartId, AddBookingToCartRequest request, BookingService bookingService, CartService cartService, CancellationToken ct) =>
            {
                var bookingResult = await ResolveBookingAsync(request, bookingService, ct).ConfigureAwait(false);
                if (!bookingResult.Success)
                {
                    return bookingResult.Error;
                }

                var added = await cartService.AddBookingToCartAsync(cartId, bookingResult.BookingId, request.Quantity, ct).ConfigureAwait(false);
                if (!added)
                {
                    return Results.BadRequest(new { message = "Unable to add booking to cart." });
                }

                var cart = await cartService.GetOrCreateCartAsync(cartId, ct).ConfigureAwait(false);
                var totals = await cartService.CalculateTotalsAsync(cartId, ct).ConfigureAwait(false);
                return Results.Ok(CartResponse.FromDomain(cart, totals));
            })
            .WithName("AddBookingToCart")
            .WithSummary("Add a booking to the cart")
            .WithDescription("Adds an existing or newly created booking to the specified cart and returns updated totals.");

        carts.MapPut("/{cartId:guid}/items/{bookingId:guid}/remove", async (Guid cartId, Guid bookingId, CartService cartService, CancellationToken ct) =>
            {
                var removed = await cartService.RemoveBookingFromCartAsync(cartId, bookingId, ct).ConfigureAwait(false);
                if (!removed)
                {
                    return Results.NotFound(new { message = "Booking not found in cart." });
                }

                var cart = await cartService.GetOrCreateCartAsync(cartId, ct).ConfigureAwait(false);
                var totals = await cartService.CalculateTotalsAsync(cartId, ct).ConfigureAwait(false);
                return Results.Ok(CartResponse.FromDomain(cart, totals));
            })
            .WithName("RemoveBookingFromCart")
            .WithSummary("Remove a booking from the cart")
            .WithDescription("Removes a booking from the specified cart and returns the updated cart summary.");
    }

    private static async Task<(bool Success, Guid BookingId, IResult Error)> ResolveBookingAsync(AddBookingToCartRequest request, BookingService service, CancellationToken ct)
    {
        if (request.BookingId.HasValue && request.BookingId != Guid.Empty)
        {
            return (true, request.BookingId.Value, Results.Empty);
        }

        if (!request.ParkId.HasValue)
        {
            return (false, Guid.Empty, Results.BadRequest(new { message = "ParkId is required when BookingId is not supplied." }));
        }

        if (!request.TryValidate(out var guestCategory, out var date, out var errors))
        {
            return (false, Guid.Empty, Results.ValidationProblem(errors));
        }

        var booking = await service.CreateSingleDayBookingAsync(request.ParkId.Value, request.GuestName, guestCategory, date, ct).ConfigureAwait(false);
        if (booking is null)
        {
            return (false, Guid.Empty, Results.BadRequest(new { message = "Unable to create booking with the supplied details." }));
        }

        return (true, booking.Id, Results.Empty);
    }

    private sealed record CartResponse(
        Guid CartId,
        DateTime CreatedAtUtc,
        DateTime LastUpdatedUtc,
        decimal RegularTotal,
        decimal DiscountedTotal,
        decimal Tax,
        decimal Total,
        IReadOnlyCollection<CartItemResponse> Items)
    {
        public static CartResponse FromDomain(Cart cart, CartTotals totals)
        {
            return new CartResponse(
                cart.Id,
                cart.CreatedAtUtc,
                cart.LastUpdatedUtc,
                totals.RegularTotal.Amount,
                totals.DiscountedTotal.Amount,
                totals.Tax.Amount,
                totals.TotalWithTax.Amount,
                totals.Items.Select(CartItemResponse.FromDomain).ToList().AsReadOnly());
        }

    }

    private sealed record CartItemResponse(
        Guid BookingId,
        string Park,
        int Quantity,
        decimal UnitPrice,
        decimal Subtotal,
        string Currency)
    {
        public static CartItemResponse FromDomain(CartItem item)
        {
            return new CartItemResponse(
                item.BookingId,
                item.ParkName,
                item.Quantity,
                item.UnitPrice.Amount,
                item.Subtotal.Amount,
                item.UnitPrice.Currency);
        }
    }

    private sealed class AddBookingToCartRequest
    {
        public Guid? BookingId { get; init; }
        public Guid? ParkId { get; init; }
        public string GuestName { get; init; } = "Guest";
        public string GuestCategoryName { get; init; } = nameof(GuestCategory.Adult);
        public string? Date { get; init; }
        public int Quantity { get; init; } = 1;

        public bool TryValidate(out GuestCategory category, out DateOnly date, out Dictionary<string, string[]> errors)
        {
            errors = new Dictionary<string, string[]>();
            category = GuestCategory.Adult;
            date = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            if (Quantity <= 0)
            {
                errors["Quantity"] = new[] { "Quantity must be greater than zero." };
            }

            if (!Enum.TryParse<GuestCategory>(GuestCategoryName, true, out var parsedCategory))
            {
                errors["GuestCategory"] = new[] { "Guest category must be Adult or Child." };
            }
            else
            {
                category = parsedCategory;
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
