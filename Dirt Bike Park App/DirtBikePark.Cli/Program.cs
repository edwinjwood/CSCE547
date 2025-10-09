using System.Text.Json.Serialization;
using DirtBikePark.Cli.App;
using DirtBikePark.Cli.Data;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.Services;
using DirtBikePark.Cli.Infrastructure.Repositories;
using DirtBikePark.Cli.Infrastructure.Storage;
using DirtBikePark.Cli.Presentation.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

var databasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "dirtbikepark.db"));
var dbContextFactory = new DirtBikeParkDbContextFactory(databasePath);
await dbContextFactory.EnsureCreatedAsync();

if (args.Any(a => string.Equals(a, "--cli", StringComparison.OrdinalIgnoreCase)))
{
	// Preserve the original interactive CLI experience when explicitly requested.
	var cliApp = new Application();
	await cliApp.RunAsync();
	return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Dirt Bike Park API",
		Version = "v1",
		Description = "Endpoints for managing parks, bookings, and carts"
	});
});

builder.Services.AddSingleton(dbContextFactory);
builder.Services.AddScoped<IParkRepository, SqliteParkRepository>();
builder.Services.AddScoped<IBookingRepository, SqliteBookingRepository>();
builder.Services.AddScoped<ICartRepository, SqliteCartRepository>();
builder.Services.AddScoped<ParkService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<CartService>();

var webApp = builder.Build();

await SeedDataAsync(webApp.Services).ConfigureAwait(false);

webApp.UseSwagger();
webApp.UseSwaggerUI();

webApp.MapParkEndpoints();
webApp.MapBookingEndpoints();
webApp.MapCartEndpoints();

webApp.Run();

static async Task SeedDataAsync(IServiceProvider services)
{
	using var scope = services.CreateScope();
	var repository = scope.ServiceProvider.GetRequiredService<IParkRepository>();
	await DataSeeder.SeedParksAsync(repository).ConfigureAwait(false);
}
