using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using DirtBikePark.Cli.Domain.Services;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Presentation.Api;

/// <summary>
/// Provides REST endpoints for checkout and payment processing.
/// </summary>
public static class CheckoutEndpoints
{
    public static RouteGroupBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/checkout")
            .WithTags("Checkout");

        group.MapPost("/", async (CheckoutRequest request, PaymentService paymentService, CartService cartService, BookingService bookingService, CancellationToken cancellationToken) =>
            {
                // Step 1: Validate the request
                if (!request.TryValidate(out var errors))
                {
                    return Results.ValidationProblem(errors);
                }

                // Step 2: Ensure cart exists even if client-side cart items are not stored server-side
                var cart = await cartService.GetOrCreateCartAsync(request.CartId, cancellationToken).ConfigureAwait(false);

                // Step 3: Persist bookings from cart items into the server cart
                foreach (var item in request.Items)
                {
                    var booking = await bookingService.CreateBookingForCartItemAsync(
                        item.ParkId,
                        request.CardholderName,
                        item.Adults,
                        item.Kids,
                        item.DayCount,
                        cancellationToken).ConfigureAwait(false);

                    if (booking is null)
                    {
                        return Results.BadRequest(new { message = "Unable to create booking for one or more cart items (capacity/dates unavailable)." });
                    }

                    await cartService.AddBookingToCartAsync(cart.Id, booking.Id, quantity: 1, cancellationToken).ConfigureAwait(false);
                }

                // Step 4: Create billing address from request
                var billingAddress = new Address(
                    request.Street,
                    request.City,
                    request.State,
                    request.PostalCode);

                // Step 5: Process payment (cart totals may be zero when the UI manages items client-side)
                var paymentResult = await paymentService.ProcessPaymentAsync(
                    cart.Id,
                    request.CardholderName,
                    request.CardNumber,
                    request.ExpirationMonthYear,
                    request.Cvc,
                    billingAddress,
                    cancellationToken).ConfigureAwait(false);

                // Step 6: Return response
                return Results.Ok(new CheckoutResponse(
                    Success: paymentResult.Success,
                    Message: paymentResult.Message));
            })
            .WithName("ProcessPayment")
            .WithSummary("Process payment and checkout cart")
            .WithDescription("Processes payment for a cart containing bookings. On success, all bookings are confirmed and cart is cleared.");

        return group;
    }

    /// <summary>
    /// Request DTO for checkout/payment processing.
    /// </summary>
    private sealed class CheckoutRequest
    {
        public Guid CartId { get; init; }
        public string CardholderName { get; init; } = string.Empty;
        public string CardNumber { get; init; } = string.Empty;
        public string ExpirationMonthYear { get; init; } = string.Empty;
        public string Cvc { get; init; } = string.Empty;
        public string Street { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public List<CartItemRequest> Items { get; init; } = new();

        public bool TryValidate(out Dictionary<string, string[]> errors)
        {
            errors = new Dictionary<string, string[]>();

            if (CartId == Guid.Empty)
            {
                errors["CartId"] = new[] { "Cart ID is required." };
            }

            if (string.IsNullOrWhiteSpace(CardholderName))
            {
                errors["CardholderName"] = new[] { "Cardholder name is required." };
            }

            if (string.IsNullOrWhiteSpace(CardNumber))
            {
                errors["CardNumber"] = new[] { "Card number is required." };
            }
            else
            {
                var digitsOnly = new string(System.Linq.Enumerable.Where(CardNumber, char.IsDigit).ToArray());
                if (digitsOnly.Length < 13 || digitsOnly.Length > 19)
                {
                    errors["CardNumber"] = new[] { "Card number must be between 13 and 19 digits." };
                }
            }

            if (string.IsNullOrWhiteSpace(ExpirationMonthYear))
            {
                errors["ExpirationMonthYear"] = new[] { "Expiration date is required (MM/YY format)." };
            }
            else if (!ValidateExpiration(ExpirationMonthYear))
            {
                errors["ExpirationMonthYear"] = new[] { "Expiration date must be in MM/YY format." };
            }

            if (string.IsNullOrWhiteSpace(Cvc))
            {
                errors["Cvc"] = new[] { "CVC is required." };
            }
            else if (!Cvc.All(char.IsDigit) || (Cvc.Length < 3 || Cvc.Length > 4))
            {
                errors["Cvc"] = new[] { "CVC must be 3-4 digits." };
            }

            if (string.IsNullOrWhiteSpace(Street))
            {
                errors["Street"] = new[] { "Street is required." };
            }

            if (string.IsNullOrWhiteSpace(City))
            {
                errors["City"] = new[] { "City is required." };
            }

            if (string.IsNullOrWhiteSpace(State))
            {
                errors["State"] = new[] { "State is required." };
            }

            if (string.IsNullOrWhiteSpace(PostalCode))
            {
                errors["PostalCode"] = new[] { "Postal code is required." };
            }

            if (Items is null || Items.Count == 0)
            {
                errors["Items"] = new[] { "At least one cart item is required." };
            }
            else
            {
                for (var i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    if (item.ParkId == Guid.Empty)
                    {
                        errors[$"Items[{i}].ParkId"] = new[] { "ParkId is required." };
                    }

                    if (item.DayCount <= 0)
                    {
                        errors[$"Items[{i}].DayCount"] = new[] { "DayCount must be at least 1." };
                    }

                    if (item.Adults + item.Kids <= 0)
                    {
                        errors[$"Items[{i}].Guests"] = new[] { "At least one guest (adult or child) is required." };
                    }
                }
            }

            return errors.Count == 0;
        }

        private static bool ValidateExpiration(string expirationMonthYear)
        {
            var parts = expirationMonthYear.Split('/');
            if (parts.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out var month) || month < 1 || month > 12)
            {
                return false;
            }

            if (!int.TryParse(parts[1], out var year) || year < 0 || year > 99)
            {
                return false;
            }

            return true;
        }
    }

    private sealed class CartItemRequest
    {
        public Guid ParkId { get; init; }
        public int Adults { get; init; }
        public int Kids { get; init; }
        public int DayCount { get; init; } = 1;
    }

    /// <summary>
    /// Response DTO for checkout/payment processing.
    /// </summary>
    private sealed record CheckoutResponse(
        bool Success,
        string Message);
}
