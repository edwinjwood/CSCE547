using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Services;
using DirtBikePark.Cli.Presentation;
using DirtBikePark.Cli.Presentation.CliPages;

namespace DirtBikePark.Cli.App;

/// <summary>
/// Routes user input to the appropriate CLI pages and orchestrates the command loop.
/// </summary>
public class CommandRouter
{
    private readonly ParkService _parkService;
    private readonly BookingService _bookingService;
    private readonly CartService _cartService;
    private readonly PaymentService _paymentService;
    private readonly MenuRenderer _menuRenderer;
    private readonly WelcomePage _welcomePage;
    private readonly ParksPage _parksPage;
    private readonly BookingsPage _bookingsPage;
    private readonly CartPage _cartPage;
    private readonly CheckoutPage _checkoutPage;

    public CommandRouter(
        ParkService parkService,
        BookingService bookingService,
        CartService cartService,
        PaymentService paymentService,
        MenuRenderer menuRenderer)
    {
        _parkService = parkService;
        _bookingService = bookingService;
        _cartService = cartService;
        _paymentService = paymentService;
        _menuRenderer = menuRenderer;
        _welcomePage = new WelcomePage(_parkService, _menuRenderer);
        _parksPage = new ParksPage(_parkService, _menuRenderer);
    _bookingsPage = new BookingsPage(_bookingService, _parkService, _cartService, _menuRenderer);
        _cartPage = new CartPage(_cartService, _menuRenderer);
        _checkoutPage = new CheckoutPage(_cartService, _paymentService, _menuRenderer);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var initialSelection = await _welcomePage.ShowAsync(cancellationToken).ConfigureAwait(false);
        if (initialSelection.HasValue)
        {
            var result = await _bookingsPage.StartInteractiveBookingAsync(initialSelection.Value, cancellationToken).ConfigureAwait(false);
            await HandleBookingFlowResultAsync(result, cancellationToken).ConfigureAwait(false);
        }
        while (!cancellationToken.IsCancellationRequested)
        {
            _menuRenderer.ShowPrompt();
            var input = Console.ReadLine();
            if (input is null)
            {
                continue;
            }

            var command = input.Trim();
            if (string.Equals(command, "quit", StringComparison.OrdinalIgnoreCase))
            {
                _menuRenderer.ShowInfo("Goodbye!");
                break;
            }

            if (string.Equals(command, "help", StringComparison.OrdinalIgnoreCase))
            {
                _menuRenderer.ShowHelp();
                continue;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            await RouteAsync(command, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RouteAsync(string command, CancellationToken cancellationToken)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        try
        {
            switch (parts[0].ToLowerInvariant())
            {
                case "parks":
                    await _parksPage.HandleAsync(parts.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                    break;
                case "bookings":
                    await _bookingsPage.HandleAsync(parts.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                    break;
                case "makebooking":
                    await _bookingsPage.HandleMakeBookingAsync(parts.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                    break;
                case "start":
                case "welcome":
                    var selection = await _welcomePage.ShowAsync(cancellationToken).ConfigureAwait(false);
                    if (selection.HasValue)
                    {
                        var flowResult = await _bookingsPage.StartInteractiveBookingAsync(selection.Value, cancellationToken).ConfigureAwait(false);
                        await HandleBookingFlowResultAsync(flowResult, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case "cart":
                    await _cartPage.HandleAsync(parts.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                    _checkoutPage.SetActiveCart(_cartPage.ActiveCartId);
                    break;
                case "checkout":
                    _checkoutPage.SetActiveCart(_cartPage.ActiveCartId);
                    await _checkoutPage.HandleAsync(Array.Empty<string>(), cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    _menuRenderer.ShowError("Unknown command. Type 'help' to see available commands.");
                    break;
            }
        }
        catch (Exception ex)
        {
            _menuRenderer.ShowError(ex.Message);
        }
    }

    private async Task HandleBookingFlowResultAsync(BookingsPage.BookingFlowResult result, CancellationToken cancellationToken)
    {
        if (result.CartId.HasValue)
        {
            _cartPage.SetActiveCart(result.CartId.Value);
            _checkoutPage.SetActiveCart(result.CartId.Value);
        }

        switch (result.Action)
        {
            case BookingsPage.BookingFlowAction.ViewCart:
                await _cartPage.HandleAsync(Array.Empty<string>(), cancellationToken).ConfigureAwait(false);
                if (_cartPage.ActiveCartId != Guid.Empty)
                {
                    _checkoutPage.SetActiveCart(_cartPage.ActiveCartId);
                }
                break;

            case BookingsPage.BookingFlowAction.Checkout:
                await _checkoutPage.HandleAsync(Array.Empty<string>(), cancellationToken).ConfigureAwait(false);
                break;

            case BookingsPage.BookingFlowAction.ChooseAnotherPark:
                var nextSelection = await _welcomePage.ShowAsync(cancellationToken).ConfigureAwait(false);
                if (nextSelection.HasValue)
                {
                    var followUp = await _bookingsPage.StartInteractiveBookingAsync(nextSelection.Value, cancellationToken).ConfigureAwait(false);
                    await HandleBookingFlowResultAsync(followUp, cancellationToken).ConfigureAwait(false);
                }
                break;

            default:
                break;
        }
    }
}
