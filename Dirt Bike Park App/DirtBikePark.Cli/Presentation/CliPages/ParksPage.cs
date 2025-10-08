using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Services;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Presentation.CliPages;

/// <summary>
/// Handles park-related commands.
/// </summary>
public class ParksPage : CliPageBase
{
    private readonly ParkService _parkService;

    public ParksPage(ParkService parkService, MenuRenderer menu) : base(menu)
    {
        _parkService = parkService;
    }

    public async Task HandleAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            Menu.ShowError("Missing park command. Use 'parks list' or 'parks view <id>'.");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                await ListParksAsync(cancellationToken).ConfigureAwait(false);
                break;
            case "view":
                await ViewParkAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            case "add":
                await AddParkAsync(cancellationToken).ConfigureAwait(false);
                break;
            case "remove":
                await RemoveParkAsync(args.Skip(1).ToArray(), cancellationToken).ConfigureAwait(false);
                break;
            default:
                Menu.ShowError("Unknown parks command.");
                break;
        }
    }

    private async Task ListParksAsync(CancellationToken cancellationToken)
    {
        var parks = await _parkService.GetParksAsync(cancellationToken).ConfigureAwait(false);
        if (parks.Count == 0)
        {
            Menu.ShowInfo("No parks available yet.");
            return;
        }

        foreach (var park in parks)
        {
            RenderParkSummary(park);
        }
    }

    private async Task ViewParkAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || !TryParseGuid(args[0], out var id))
        {
            Menu.ShowError("Please supply a valid park ID.");
            return;
        }

        var park = await _parkService.GetParkAsync(id, cancellationToken).ConfigureAwait(false);
        if (park is null)
        {
            Menu.ShowError("Park not found.");
            return;
        }

        RenderParkDetails(park);
    }

    private async Task AddParkAsync(CancellationToken cancellationToken)
    {
        Console.Write("Park Name: ");
        var name = Console.ReadLine() ?? string.Empty;
        Console.Write("Description: ");
        var description = Console.ReadLine() ?? string.Empty;
        Console.Write("Location: ");
        var location = Console.ReadLine() ?? string.Empty;
        Console.Write("Guest Limit: ");
        if (!int.TryParse(Console.ReadLine(), out var guestLimit) || guestLimit <= 0)
        {
            Menu.ShowError("Guest limit must be a positive number.");
            return;
        }

        Console.Write("Price per guest per day: ");
        if (!decimal.TryParse(Console.ReadLine(), out var price) || price <= 0)
        {
            Menu.ShowError("Price must be a positive number.");
            return;
        }

        var availability = Enumerable.Range(0, 14)
            .Select(offset => DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(offset))
            .ToList();

        var park = new Park(
            Guid.NewGuid(),
            name,
            description,
            location,
            guestLimit,
            new Money(price),
            availability);

        await _parkService.AddParkAsync(park, cancellationToken).ConfigureAwait(false);
        Menu.ShowSuccess("Park added successfully.");
    }

    private async Task RemoveParkAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || !TryParseGuid(args[0], out var id))
        {
            Menu.ShowError("Please supply a valid park ID.");
            return;
        }

        var removed = await _parkService.RemoveParkAsync(id, cancellationToken).ConfigureAwait(false);
        if (removed)
        {
            Menu.ShowSuccess("Park removed successfully.");
        }
        else
        {
            Menu.ShowError("Park could not be removed (maybe bookings exist or ID invalid).");
        }
    }

    private void RenderParkSummary(Park park)
    {
        Console.WriteLine($"[{park.Id}] {park.Name} - {park.Location} (Capacity: {park.AvailableGuestCapacity}/{park.GuestLimit})");
    }

    private void RenderParkDetails(Park park)
    {
        Console.WriteLine("------------------------------------------");
        Console.WriteLine(park.Name);
        Console.WriteLine(park.Description);
        Console.WriteLine($"Location: {park.Location}");
        Console.WriteLine($"Capacity Available: {park.AvailableGuestCapacity}/{park.GuestLimit}");
        Console.WriteLine($"Price per Adult: {park.PricePerGuestPerDay:C}");
        Console.WriteLine($"Price per Child: {(park.PricePerGuestPerDay * 0.6m):C}");
        Console.WriteLine("Upcoming Availability:");

        var upcoming = park.AvailableDates.Take(7).ToList();
        if (upcoming.Count == 0)
        {
            Console.WriteLine("  - No dates available.");
        }
        else
        {
            for (var i = 0; i < upcoming.Count; i++)
            {
                var dayNumber = i + 1;
                var date = upcoming[i];
                Console.WriteLine($"  Day {dayNumber}: {date:MMM dd, yyyy}");
            }
            Console.WriteLine();
            Console.WriteLine("Tip: type 'welcome' to pick a park without IDs or 'makebooking <parkId>' for advanced commands.");
        }
        Console.WriteLine("------------------------------------------");
    }
}
