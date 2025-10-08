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

    public Task<Cart> GetOrCreateCartAsync(Guid? cartId = null, CancellationToken cancellationToken = default)
        => _cartRepository.GetOrCreateAsync(cartId, cancellationToken);

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

    public Task<bool> UndoLastChangeAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        return ModifyCartAsync(cartId, cart => cart.Undo(), cancellationToken);
    }

    public Task<bool> ClearCartAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        return ModifyCartAsync(cartId, cart =>
        {
            cart.Clear();
            return true;
        }, cancellationToken);
    }

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

    private static Money CalculateRegularTotal(IEnumerable<CartItem> items)
    {
        var total = items.Sum(item => item.Subtotal.Amount);
        return new Money(total);
    }

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

    private static Money CalculateTax(Money amount)
    {
        const decimal TaxRate = 0.0825m;
        return new Money(amount.Amount * TaxRate, amount.Currency);
    }

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

public readonly record struct CartTotals(
    Money RegularTotal,
    Money DiscountedTotal,
    Money Tax,
    Money TotalWithTax,
    IReadOnlyCollection<CartItem> Items);
