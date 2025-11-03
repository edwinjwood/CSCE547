using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Services;

/// <summary>
/// Handles cart lifecycle and pricing aggregation.
/// </summary>
public class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IParkRepository _parkRepository;

    public CartService(ICartRepository cartRepository, IBookingRepository bookingRepository, IParkRepository parkRepository)
    {
        _cartRepository = cartRepository;
        _bookingRepository = bookingRepository;
        _parkRepository = parkRepository;
    }

    //Retrieves an existing cart by its id or creates a new one.
    public Task<Cart> GetOrCreateCartAsync(Guid? cartId = null, CancellationToken cancellationToken = default)
        => _cartRepository.GetOrCreateAsync(cartId, cancellationToken);

    //Adds a booking to the cart with the specified quantity. Returns false if the booking does not exist or quantity is invalid.
    public async Task<bool> AddBookingToCartAsync(Guid cartId, Guid bookingId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return false;
        }

        var cart = await _cartRepository.GetOrCreateAsync(cartId, cancellationToken).ConfigureAwait(false);
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken).ConfigureAwait(false);
        if (booking is null)
        {
            return false;
        }

        var park = await _parkRepository.GetByIdAsync(booking.ParkId, cancellationToken).ConfigureAwait(false);
        var parkName = park?.Name ?? booking.ParkId.ToString();
        var item = new CartItem(booking.Id, parkName, quantity, booking.TotalPrice);
        cart.AddOrUpdateItem(item);
        await _cartRepository.UpdateAsync(cart, cancellationToken).ConfigureAwait(false);
        return true;
    }

    //Removes a booking from the cart by its bookingId.
    public async Task<bool> RemoveBookingFromCartAsync(Guid cartId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetOrCreateAsync(cartId, cancellationToken).ConfigureAwait(false);
        var removed = cart.RemoveItem(bookingId);
        if (removed)
        {
            await _cartRepository.UpdateAsync(cart, cancellationToken).ConfigureAwait(false);
        }

        return removed;
    }
    //Undoes the last change made to the cart.
    public Task<bool> UndoLastChangeAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        return ModifyCartAsync(cartId, cart => cart.Undo(), cancellationToken);
    }

    //Clears all items from the cart.
    public Task<bool> ClearCartAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        return ModifyCartAsync(cartId, cart =>
        {
            cart.Clear();
            return true;
        }, cancellationToken);
    }

    //Calculates totals for the cart taking into account any bundles or taxes.
    public async Task<CartTotals> CalculateTotalsAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetOrCreateAsync(cartId, cancellationToken).ConfigureAwait(false);
        var items = cart.Items;

        var regularTotal = cart.Total(CalculateRegularTotal);
        var bundleTotal = ApplyBundleDiscount(regularTotal, items);
        var tax = CalculateTax(bundleTotal);
        var totalWithTax = bundleTotal + tax;

        return new CartTotals(regularTotal, bundleTotal, tax, totalWithTax, items);
    }

    //Calculates the total of all item values without discounts or taxes
    private static Money CalculateRegularTotal(IEnumerable<CartItem> items)
    {
        var total = items.Sum(item => item.Subtotal.Amount);
        return new Money(total);
    }

    //Applies a bundle discount if the cart meets the criteria.
    private static Money ApplyBundleDiscount(Money regularTotal, IReadOnlyCollection<CartItem> items)
    {
        const int BundleTrigger = 3;
        const decimal DiscountRate = 0.10m;

        if (items.Count >= BundleTrigger)
        {
            var discount = regularTotal.Amount * DiscountRate;
            return regularTotal - new Money(discount);
        }

        return regularTotal;
    }

    //Calculates the tax amount. Tax rate is assumed to be 8.25%.
    private static Money CalculateTax(Money amount)
    {
        const decimal TaxRate = 0.0825m;
        return new Money(amount.Amount * TaxRate, amount.Currency);
    }

    //Function to modify the cart. Retrieves the cart, applies a specific action, and updates the cart if the action was successful.
    private async Task<bool> ModifyCartAsync(Guid cartId, Func<Cart, bool> action, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetOrCreateAsync(cartId, cancellationToken).ConfigureAwait(false);
        var result = action(cart);
        if (result)
        {
            await _cartRepository.UpdateAsync(cart, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}

//Encapsulates the possible totals for a cart.
public readonly record struct CartTotals(
    Money RegularTotal,
    Money DiscountedTotal,
    Money Tax,
    Money TotalWithTax,
    IReadOnlyCollection<CartItem> Items);
