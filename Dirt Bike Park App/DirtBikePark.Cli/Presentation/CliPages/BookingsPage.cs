using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Enums;
using DirtBikePark.Cli.Domain.Services;

namespace DirtBikePark.Cli.Presentation.CliPages;

/// <summary>
/// Handles booking-related commands.
/// </summary>
public class BookingsPage : CliPageBase
{
    private readonly BookingService _bookingService;
    private readonly ParkService _parkService;
    private readonly CartService _cartService;
    private Guid _activeCartId;

    public Guid? ActiveCartId => _activeCartId == Guid.Empty ? null : _activeCartId;

    public BookingsPage(BookingService bookingService, ParkService parkService, CartService cartService, MenuRenderer menu) : base(menu)
    {
        _bookingService = bookingService;
        _parkService = parkService;
        _cartService = cartService;
    }

    public sealed record BookingFlowResult(BookingFlowAction Action, Guid? CartId)
    {
        public static BookingFlowResult ReturnToMenu(Guid? cartId) => new(BookingFlowAction.ReturnToMenu, cartId);
        public static BookingFlowResult ChooseAnotherPark(Guid? cartId) => new(BookingFlowAction.ChooseAnotherPark, cartId);
        public static BookingFlowResult ViewCart(Guid? cartId) => new(BookingFlowAction.ViewCart, cartId);
        public static BookingFlowResult Checkout(Guid? cartId) => new(BookingFlowAction.Checkout, cartId);
    }

    public enum BookingFlowAction
    {
        ContinueInPark,
        ReturnToMenu,
        ChooseAnotherPark,
        ViewCart,
        Checkout
    }

    public async Task HandleAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            Menu.ShowError("Missing booking command. Use 'bookings list' or 'bookings create <parkId>'.");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                await ListBookingsAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            case "create":
                await CreateBookingAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            case "cancel":
                await CancelBookingAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            case "remove":
                await RemoveBookingAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            default:
                Menu.ShowError("Unknown bookings command.");
                break;
        }
    }

    public async Task HandleMakeBookingAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || !TryParseGuid(args[0], out var parkId))
        {
            Menu.ShowError("Usage: makebooking <parkId> [dayNumber] [adult|child]");
            return;
        }

        var park = await _parkService.GetParkAsync(parkId, cancellationToken).ConfigureAwait(false);
        if (park is null)
        {
            Menu.ShowError("Park not found.");
            return;
        }

        var availableDates = park.AvailableDates.OrderBy(d => d).Take(7).ToList();
        if (args.Length == 1)
        {
            RenderParkBookingOptions(park, availableDates);
            Console.WriteLine("Enter 'makebooking <parkId> <dayNumber> <adult|child>' for a direct booking or use 'welcome' to choose from the start.");
            return;
        }

        if (!int.TryParse(args[1], out var dayIndex) || dayIndex <= 0)
        {
            Menu.ShowError("Day number must be a positive integer (1-7).");
            return;
        }

        if (dayIndex > availableDates.Count)
        {
            Menu.ShowError("Selected day is no longer available.");
            return;
        }

        var category = GuestCategory.Adult;
        if (args.Length >= 3)
        {
            category = args[2].Equals("child", StringComparison.OrdinalIgnoreCase)
                ? GuestCategory.Child
                : GuestCategory.Adult;
        }

        var selectedDate = availableDates[dayIndex - 1];
        Console.Write("Guest Name [Guest]: ");
        var guestName = Console.ReadLine() ?? string.Empty;

        var booking = await _bookingService.CreateSingleDayBookingAsync(
            parkId,
            guestName,
            category,
            selectedDate,
            cancellationToken).ConfigureAwait(false);

        if (booking is null)
        {
            Menu.ShowError("Unable to create booking. The slot may have been taken.");
            return;
        }

        Menu.ShowSuccess($"Booking confirmed for {selectedDate:MMM dd, yyyy} ({category}) with confirmation ID {booking.Id}.");
    }

    public async Task<BookingFlowResult> StartInteractiveBookingAsync(Guid parkId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var park = await _parkService.GetParkAsync(parkId, cancellationToken).ConfigureAwait(false);
            if (park is null)
            {
                Menu.ShowError("Park not found.");
                return BookingFlowResult.ReturnToMenu(ActiveCartId);
            }

            var upcomingDates = park.AvailableDates.OrderBy(d => d).Take(7).ToList();
            RenderParkBookingOptions(park, upcomingDates);

            Console.WriteLine("Enter a day number to add it to your trip.");
            Console.WriteLine("You can also type 'cart' for a quick summary, 'checkout' to pay, or 'parks' to pick another park.");
            Console.Write("Press Enter to return to the main menu: ");

            var input = Console.ReadLine();
            if (input is null)
            {
                continue;
            }

            var trimmed = input.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return BookingFlowResult.ReturnToMenu(ActiveCartId);
            }

            switch (trimmed.ToLowerInvariant())
            {
                case "cart":
                    if (ActiveCartId.HasValue)
                    {
                        await ShowCartSummaryAsync(ActiveCartId.Value, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        Menu.ShowInfo("Your cart is currently empty. Add a booking first.");
                    }
                    continue;

                case "checkout":
                    if (!ActiveCartId.HasValue)
                    {
                        Menu.ShowInfo("Your cart is empty. Add a booking before checking out.");
                        continue;
                    }
                    return BookingFlowResult.Checkout(ActiveCartId);

                case "parks":
                case "back":
                    return BookingFlowResult.ChooseAnotherPark(ActiveCartId);

                case "menu":
                    return BookingFlowResult.ReturnToMenu(ActiveCartId);
            }

            if (!int.TryParse(trimmed, out var dayIndex) || upcomingDates.Count == 0)
            {
                Menu.ShowError("Please enter a valid day number.");
                continue;
            }

            if (dayIndex <= 0 || dayIndex > upcomingDates.Count)
            {
                Menu.ShowError($"Please enter a number between 1 and {upcomingDates.Count}.");
                continue;
            }

            var category = PromptForCategory();
            var guestName = PromptForGuestName();
            var selectedDate = upcomingDates[dayIndex - 1];

            var booking = await _bookingService.CreateSingleDayBookingAsync(
                parkId,
                guestName,
                category,
                selectedDate,
                cancellationToken).ConfigureAwait(false);

            if (booking is null)
            {
                Menu.ShowError("Unable to create booking. The slot may have been taken.");
                continue;
            }

            Menu.ShowSuccess($"Booking confirmed for {selectedDate:MMM dd, yyyy} ({category}). Reference: {booking.Id}");

            if (PromptYesNo("Add this booking to your cart? (Y/n): "))
            {
                var cartId = await EnsureActiveCartAsync(cancellationToken).ConfigureAwait(false);
                var added = await _cartService.AddBookingToCartAsync(cartId, booking.Id, 1, cancellationToken).ConfigureAwait(false);
                if (added)
                {
                    Menu.ShowSuccess("Booking added to cart.");
                    await ShowCartSummaryAsync(cartId, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Menu.ShowError("Failed to add booking to cart.");
                }
            }

            var action = PromptPostBookingAction();
            switch (action)
            {
                case BookingFlowAction.ContinueInPark:
                    continue;
                case BookingFlowAction.ChooseAnotherPark:
                    return BookingFlowResult.ChooseAnotherPark(ActiveCartId);
                case BookingFlowAction.ViewCart:
                    if (ActiveCartId.HasValue)
                    {
                        return BookingFlowResult.ViewCart(ActiveCartId);
                    }
                    Menu.ShowInfo("Your cart is currently empty.");
                    break;
                case BookingFlowAction.Checkout:
                    if (ActiveCartId.HasValue)
                    {
                        return BookingFlowResult.Checkout(ActiveCartId);
                    }
                    Menu.ShowInfo("Your cart is currently empty.");
                    break;
                case BookingFlowAction.ReturnToMenu:
                    return BookingFlowResult.ReturnToMenu(ActiveCartId);
                default:
                    continue;
            }
        }

        return BookingFlowResult.ReturnToMenu(ActiveCartId);
    }

    private async Task ListBookingsAsync(string[] args, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Booking> bookings;
        if (args.Length > 0 && TryParseGuid(args[0], out var parkId))
        {
            bookings = await _bookingService.GetBookingsByParkAsync(parkId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            bookings = await _bookingService.GetAllBookingsAsync(cancellationToken).ConfigureAwait(false);
        }

        if (bookings.Count == 0)
        {
            Menu.ShowInfo("No bookings found.");
            return;
        }

        foreach (var booking in bookings)
        {
            RenderBookingSummary(booking);
        }
    }

    private async Task CreateBookingAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || !TryParseGuid(args[0], out var parkId))
        {
            Menu.ShowError("Please supply a valid park ID.");
            return;
        }

        Console.Write("Guest Name: ");
        var guestName = Console.ReadLine() ?? string.Empty;
        Console.Write("Number of Guests: ");
        if (!int.TryParse(Console.ReadLine(), out var guests) || guests <= 0)
        {
            Menu.ShowError("Guest count must be a positive integer.");
            return;
        }

        Console.Write("Number of Days: ");
        if (!int.TryParse(Console.ReadLine(), out var days) || days <= 0)
        {
            Menu.ShowError("Number of days must be a positive integer.");
            return;
        }

        var booking = await _bookingService.CreateBookingAsync(parkId, guestName, guests, days, cancellationToken).ConfigureAwait(false);
        if (booking is null)
        {
            Menu.ShowError("Unable to create booking. Check park availability.");
            return;
        }

        Menu.ShowSuccess($"Booking created with ID {booking.Id}.");
    }

    private async Task CancelBookingAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || !TryParseGuid(args[0], out var bookingId))
        {
            Menu.ShowError("Please supply a valid booking ID.");
            return;
        }

        var success = await _bookingService.CancelBookingAsync(bookingId, cancellationToken).ConfigureAwait(false);
        Menu.ShowInfo(success ? "Booking cancelled." : "Booking could not be cancelled.");
    }

    private async Task RemoveBookingAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || !TryParseGuid(args[0], out var bookingId))
        {
            Menu.ShowError("Please supply a valid booking ID.");
            return;
        }

        var success = await _bookingService.RemoveBookingAsync(bookingId, cancellationToken).ConfigureAwait(false);
        Menu.ShowInfo(success ? "Booking removed." : "Booking could not be removed.");
    }

    private async Task<Guid> EnsureActiveCartAsync(CancellationToken cancellationToken)
    {
        if (_activeCartId != Guid.Empty)
        {
            return _activeCartId;
        }

        var cart = await _cartService.GetOrCreateCartAsync(null, cancellationToken).ConfigureAwait(false);
        _activeCartId = cart.Id;
        return _activeCartId;
    }

    private async Task ShowCartSummaryAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var totals = await _cartService.CalculateTotalsAsync(cartId, cancellationToken).ConfigureAwait(false);
        Console.WriteLine();
        Console.WriteLine("Cart Summary");
        Console.WriteLine("------------");
        if (totals.Items.Count == 0)
        {
            Console.WriteLine("Your cart is empty.");
        }
        else
        {
            foreach (var item in totals.Items)
            {
                Console.WriteLine($"- {item.ParkName} (Booking {item.BookingId}) x{item.Quantity} @ {item.UnitPrice} => {item.Subtotal}");
            }
        }
        Console.WriteLine($"Subtotal: {totals.RegularTotal}");
        Console.WriteLine($"Bundle Total: {totals.DiscountedTotal}");
        Console.WriteLine($"Tax: {totals.Tax}");
        Console.WriteLine($"Total Due: {totals.TotalWithTax}");
        Console.WriteLine();
    }

    private bool PromptYesNo(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var response = Console.ReadLine();
            if (response is null)
            {
                continue;
            }

            var trimmed = response.Trim();
            if (trimmed.Length == 0 || trimmed.Equals("y", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (trimmed.Equals("n", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Menu.ShowError("Please answer 'y' or 'n'.");
        }
    }

    private BookingFlowAction PromptPostBookingAction()
    {
        Console.WriteLine();
        Console.WriteLine("What would you like to do next?");
        Console.WriteLine("  1. Book another day at this park");
        Console.WriteLine("  2. Choose a different park");
        Console.WriteLine("  3. View cart and manage items");
        Console.WriteLine("  4. Checkout");
        Console.WriteLine("  5. Return to main menu");
        Console.Write("Select an option (1-5): ");

        var input = Console.ReadLine();
        if (input is null)
        {
            return BookingFlowAction.ReturnToMenu;
        }

        return input.Trim() switch
        {
            "1" => BookingFlowAction.ContinueInPark,
            "2" => BookingFlowAction.ChooseAnotherPark,
            "3" => BookingFlowAction.ViewCart,
            "4" => BookingFlowAction.Checkout,
            _ => BookingFlowAction.ReturnToMenu
        };
    }

    private void RenderBookingSummary(Booking booking)
    {
        Console.WriteLine($"[{booking.Id}] Park: {booking.ParkId} Guests: {booking.Guests} Days: {booking.DayCount} Category: {booking.GuestCategory} Total: {booking.TotalPrice}");
    }

    private void RenderParkBookingOptions(Park park, IList<DateOnly> upcomingDates)
    {
        Console.WriteLine("------------------------------------------");
        Console.WriteLine(park.Name);
        Console.WriteLine(park.Description);
        Console.WriteLine($"Location: {park.Location}");
        Console.WriteLine($"Capacity Available: {park.AvailableGuestCapacity}/{park.GuestLimit}");
        Console.WriteLine($"Adult Price per Day: {park.PricePerGuestPerDay:C}");
        Console.WriteLine($"Child Price per Day: {(park.PricePerGuestPerDay * 0.6m):C}");
        Console.WriteLine();

        if (upcomingDates.Count == 0)
        {
            Console.WriteLine("No upcoming days available for booking.");
        }
        else
        {
            Console.WriteLine("Upcoming Availability:");
            for (var index = 0; index < upcomingDates.Count; index++)
            {
                var dayNumber = index + 1;
                var date = upcomingDates[index];
                Console.WriteLine($"  {dayNumber}. {date:MMM dd, yyyy}");
            }
        }

        Console.WriteLine("------------------------------------------");
    }

    private GuestCategory PromptForCategory()
    {
        while (true)
        {
            Console.Write("Guest type [adult/child] (default adult): ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.Equals("adult", StringComparison.OrdinalIgnoreCase))
            {
                return GuestCategory.Adult;
            }

            if (input.Equals("child", StringComparison.OrdinalIgnoreCase))
            {
                return GuestCategory.Child;
            }

            Menu.ShowError("Please enter 'adult' or 'child'.");
        }
    }

    private string PromptForGuestName()
    {
        Console.Write("Guest Name [Guest]: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? "Guest" : input.Trim();
    }
}
