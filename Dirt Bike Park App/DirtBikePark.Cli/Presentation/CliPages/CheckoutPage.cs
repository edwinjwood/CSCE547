using System;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Services;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Presentation.CliPages;

/// <summary>
/// Manages the checkout workflow including payment input.
/// </summary>
public class CheckoutPage : CliPageBase
{
    private readonly CartService _cartService;
    private readonly PaymentService _paymentService;
    private Guid _activeCartId;

    public CheckoutPage(CartService cartService, PaymentService paymentService, MenuRenderer menu) : base(menu)
    {
        _cartService = cartService;
        _paymentService = paymentService;
    }

    public void SetActiveCart(Guid cartId)
    {
        _activeCartId = cartId;
    }

    public async Task HandleAsync(string[] args, CancellationToken cancellationToken)
    {
        if (_activeCartId == Guid.Empty)
        {
            var cart = await _cartService.GetOrCreateCartAsync(null, cancellationToken).ConfigureAwait(false);
            _activeCartId = cart.Id;
        }

        var totals = await _cartService.CalculateTotalsAsync(_activeCartId, cancellationToken).ConfigureAwait(false);
        Console.WriteLine("Checkout");
        Console.WriteLine("========");
        Console.WriteLine($"Regular Total: {totals.RegularTotal}");
        Console.WriteLine($"Bundle Total: {totals.DiscountedTotal}");
        Console.WriteLine($"Tax: {totals.Tax}");
        Console.WriteLine($"Total Due: {totals.TotalWithTax}");

        Console.WriteLine();
        Console.WriteLine("Enter payment details:");
        Console.Write("Cardholder Name: ");
        var name = Console.ReadLine() ?? string.Empty;
        Console.Write("Card Number: ");
        var cardNumber = Console.ReadLine() ?? string.Empty;
        Console.Write("Expiration (MM/yy): ");
        var exp = Console.ReadLine() ?? string.Empty;
        Console.Write("CVC: ");
        var cvc = Console.ReadLine() ?? string.Empty;

        Console.Write("Billing Street: ");
        var street = Console.ReadLine() ?? string.Empty;
        Console.Write("City: ");
        var city = Console.ReadLine() ?? string.Empty;
        Console.Write("State: ");
        var state = Console.ReadLine() ?? string.Empty;
        Console.Write("Postal Code: ");
        var postal = Console.ReadLine() ?? string.Empty;

        var address = new Address(street, city, state, postal);
        try
        {
            var result = await _paymentService.ProcessPaymentAsync(
                _activeCartId,
                name,
                cardNumber,
                exp,
                cvc,
                address,
                cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                Menu.ShowSuccess(result.Message);
                await _cartService.ClearCartAsync(_activeCartId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Menu.ShowError(result.Message);
            }
        }
        catch (Exception ex)
        {
            Menu.ShowError(ex.Message);
        }
    }
}
