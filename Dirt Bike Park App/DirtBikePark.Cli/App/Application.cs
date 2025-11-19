using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Data;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.Services;
using DirtBikePark.Cli.Infrastructure.Payments;
using DirtBikePark.Cli.Infrastructure.Repositories;
using DirtBikePark.Cli.Infrastructure.Storage;
using DirtBikePark.Cli.Presentation;
using DirtBikePark.Cli.Presentation.CliPages;

namespace DirtBikePark.Cli.App;

/// <summary>
/// Entry point for configuring services and running the CLI experience.
/// </summary>
public class Application
{
    private readonly CommandRouter _router;

    public Application()
    {
        var databasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "dirtbikepark.db"));
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        var dbContextFactory = new DirtBikeParkDbContextFactory(databasePath);
        dbContextFactory.EnsureCreatedAsync().GetAwaiter().GetResult();

        IParkRepository parkRepository = new SqliteParkRepository(dbContextFactory);
        IBookingRepository bookingRepository = new SqliteBookingRepository(dbContextFactory);
        ICartRepository cartRepository = new SqliteCartRepository(dbContextFactory);
        IPaymentProcessor paymentProcessor = new MockPaymentProcessor();

        // Seed sample data
        DataSeeder.SeedParksAsync(parkRepository).GetAwaiter().GetResult();

        var parkService = new ParkService(parkRepository);
        var bookingService = new BookingService(parkRepository, bookingRepository);
        var cartService = new CartService(cartRepository, bookingRepository, parkRepository);
        var paymentService = new PaymentService(paymentProcessor, cartService);

        var menuRenderer = new MenuRenderer();
        _router = new CommandRouter(
            parkService,
            bookingService,
            cartService,
            paymentService,
            menuRenderer);
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        return _router.RunAsync(cancellationToken);
    }
}
