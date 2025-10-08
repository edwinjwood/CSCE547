using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Services;

namespace DirtBikePark.Cli.Presentation.CliPages;

/// <summary>
/// Renders the initial landing view for non-admin users.
/// </summary>
public class WelcomePage
{
    private readonly ParkService _parkService;
    private readonly MenuRenderer _menuRenderer;

    public WelcomePage(ParkService parkService, MenuRenderer menuRenderer)
    {
        _parkService = parkService;
        _menuRenderer = menuRenderer;
    }

    public async Task<Guid?> ShowAsync(CancellationToken cancellationToken = default)
    {
        _menuRenderer.ShowWelcomeHeader();

        Console.WriteLine("Welcome to the Dirt Bike Park Booking Site");
        Console.WriteLine();

        var parks = await _parkService.GetParksAsync(cancellationToken).ConfigureAwait(false);

        if (parks.Count == 0)
        {
            _menuRenderer.ShowInfo("No parks are currently available. Use 'parks add' to create one.");
            return null;
        }

        var orderedParks = parks
            .OrderBy(p => p.Name)
            .ToList();

        Console.WriteLine("Choose a park below to start a booking:");
        for (var index = 0; index < orderedParks.Count; index++)
        {
            var park = orderedParks[index];
            Console.WriteLine($"{index + 1}. {park.Name} - {park.Location}");
        }

        Console.WriteLine();
        Console.Write($"Select a park (1-{orderedParks.Count}) or press Enter to open the main menu: ");

        while (!cancellationToken.IsCancellationRequested)
        {
            var input = Console.ReadLine();
            if (input is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (int.TryParse(input, out var selection) &&
                selection >= 1 && selection <= orderedParks.Count)
            {
                return orderedParks[selection - 1].Id;
            }

            _menuRenderer.ShowError($"Please enter a number between 1 and {orderedParks.Count}.");
            Console.Write($"Select a park (1-{orderedParks.Count}) or press Enter to open the main menu: ");
        }

        return null;
    }
}
