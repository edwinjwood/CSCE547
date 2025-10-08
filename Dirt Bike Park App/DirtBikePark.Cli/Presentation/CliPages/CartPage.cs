using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Services;

namespace DirtBikePark.Cli.Presentation.CliPages;

/// <summary>
/// Handles cart commands including add, remove, and show operations.
/// </summary>
public class CartPage : CliPageBase
{
    private readonly CartService _cartService;
    private Guid _activeCartId;

    public Guid ActiveCartId => _activeCartId;

    public CartPage(CartService cartService, MenuRenderer menu) : base(menu)
    {
        _cartService = cartService;
    }

    public void SetActiveCart(Guid cartId)
    {
        _activeCartId = cartId;
    }

    public async Task HandleAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            await ShowCartAsync(Array.Empty<string>(), cancellationToken).ConfigureAwait(false);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "show":
                await ShowCartAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            case "add":
                await AddToCartAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            case "remove":
                await RemoveFromCartAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            case "undo":
                await UndoAsync(cancellationToken).ConfigureAwait(false);
                break;
            case "clear":
                await ClearAsync(cancellationToken).ConfigureAwait(false);
                break;
            default:
                Menu.ShowError("Unknown cart command.");
                break;
        }
    }

    private async Task ShowCartAsync(string[] args, CancellationToken cancellationToken)
    {
        Guid? cartId = null;
        if (args.Length > 0 && TryParseGuid(args[0], out var id))
        {
            cartId = id;
            _activeCartId = id;
        }

        var cart = await _cartService.GetOrCreateCartAsync(cartId, cancellationToken).ConfigureAwait(false);
        _activeCartId = cart.Id;
        await RenderCartAsync(cart, cancellationToken).ConfigureAwait(false);
    }

    private async Task AddToCartAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length < 2 || !TryParseGuid(args[0], out var bookingId) || !int.TryParse(args[1], out var quantity))
        {
            Menu.ShowError("Usage: cart add <bookingId> <quantity>");
            return;
        }

        if (_activeCartId == Guid.Empty)
        {
            var cart = await _cartService.GetOrCreateCartAsync(null, cancellationToken).ConfigureAwait(false);
            _activeCartId = cart.Id;
        }

        var success = await _cartService.AddBookingToCartAsync(_activeCartId, bookingId, quantity, cancellationToken).ConfigureAwait(false);
        if (success)
        {
            Menu.ShowSuccess("Booking added to cart.");
            var cart = await _cartService.GetOrCreateCartAsync(_activeCartId, cancellationToken).ConfigureAwait(false);
            await RenderCartAsync(cart, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Menu.ShowError("Failed to add booking to cart.");
        }
    }

    private async Task RemoveFromCartAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || !TryParseGuid(args[0], out var bookingId))
        {
            Menu.ShowError("Usage: cart remove <bookingId>");
            return;
        }

        if (_activeCartId == Guid.Empty)
        {
            Menu.ShowError("No active cart. Use 'cart show' to create one.");
            return;
        }

        var success = await _cartService.RemoveBookingFromCartAsync(_activeCartId, bookingId, cancellationToken).ConfigureAwait(false);
        if (success)
        {
            Menu.ShowSuccess("Booking removed from cart.");
        }
        else
        {
            Menu.ShowError("Booking not found in cart.");
        }
    }

    private async Task UndoAsync(CancellationToken cancellationToken)
    {
        if (_activeCartId == Guid.Empty)
        {
            Menu.ShowError("No active cart.");
            return;
        }

        var success = await _cartService.UndoLastChangeAsync(_activeCartId, cancellationToken).ConfigureAwait(false);
        Menu.ShowInfo(success ? "Reverted last cart change." : "Nothing to undo.");
    }

    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        if (_activeCartId == Guid.Empty)
        {
            Menu.ShowError("No active cart.");
            return;
        }

        await _cartService.ClearCartAsync(_activeCartId, cancellationToken).ConfigureAwait(false);
        Menu.ShowSuccess("Cart cleared.");
    }

    private async Task RenderCartAsync(Cart cart, CancellationToken cancellationToken)
    {
        var totals = await _cartService.CalculateTotalsAsync(cart.Id, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Cart: {cart.Id}");
        if (totals.Items.Count == 0)
        {
            Console.WriteLine("  (empty)");
        }
        else
        {
            foreach (var item in totals.Items)
            {
                Console.WriteLine($"  - Booking {item.BookingId} ({item.ParkName}) x{item.Quantity} @ {item.UnitPrice} => {item.Subtotal}");
            }
        }

        Console.WriteLine($"Subtotal: {totals.RegularTotal}");
        Console.WriteLine($"Bundle Total: {totals.DiscountedTotal}");
        Console.WriteLine($"Tax: {totals.Tax}");
        Console.WriteLine($"Total Due: {totals.TotalWithTax}");
        Console.WriteLine();
        Console.WriteLine("Next steps: type 'checkout' to pay now, 'welcome' to keep browsing parks, or press Enter for the main menu.");
    }
}
