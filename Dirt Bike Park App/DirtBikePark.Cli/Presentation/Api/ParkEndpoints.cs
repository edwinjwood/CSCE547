using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Presentation.Api;

/// <summary>
/// Provides REST endpoints for managing parks.
/// </summary>
public static class ParkEndpoints
{
    public static RouteGroupBuilder MapParkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/parks");

        group.MapGet("/", async (IParkRepository repository, CancellationToken cancellationToken) =>
            {
                var parks = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
                return Results.Ok(parks.Select(ParkResponse.FromDomain));
            })
            .WithName("ListParks")
            .WithSummary("Retrieve all parks")
            .WithDescription("Returns every dirt bike park currently stored in the system.");

        group.MapGet("/{id:guid}", async (Guid id, IParkRepository repository, CancellationToken cancellationToken) =>
            {
                var park = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
                return park is null
                    ? Results.NotFound()
                    : Results.Ok(ParkResponse.FromDomain(park));
            })
            .WithName("GetParkById")
            .WithSummary("Retrieve a park by identifier")
            .WithDescription("Returns a single park when the supplied identifier exists.");

        group.MapPost("/", async (CreateParkRequest request, IParkRepository repository, CancellationToken cancellationToken) =>
            {
                if (!request.TryValidate(out var errors, out var availability))
                {
                    return Results.ValidationProblem(errors);
                }

                var park = request.ToDomain(availability);
                await repository.AddAsync(park, cancellationToken).ConfigureAwait(false);
                return Results.Created($"/api/parks/{park.Id}", ParkResponse.FromDomain(park));
            })
            .WithName("CreatePark")
            .WithSummary("Create a new park")
            .WithDescription("Creates a new park resource with the supplied details.");

        group.MapPut("/{id:guid}", async (Guid id, UpdateParkRequest request, IParkRepository repository, CancellationToken cancellationToken) =>
            {
                var existing = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
                if (existing is null)
                {
                    return Results.NotFound();
                }

                if (!request.TryApply(existing, out var updated, out var errors))
                {
                    return Results.ValidationProblem(errors);
                }

                await repository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);
                return Results.Ok(ParkResponse.FromDomain(updated));
            })
            .WithName("UpdatePark")
            .WithSummary("Update an existing park")
            .WithDescription("Replaces all editable fields on an existing park.");

        group.MapDelete("/{id:guid}", async (Guid id, IParkRepository repository, CancellationToken cancellationToken) =>
            {
                var removed = await repository.RemoveAsync(id, cancellationToken).ConfigureAwait(false);
                return removed ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeletePark")
            .WithSummary("Delete a park")
            .WithDescription("Removes a park when the supplied identifier exists.");

        return group;
    }

    private static string Normalize(string value) => value.Trim();

    private static string NormalizeCurrency(string? currency)
        => string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();

    private sealed record ParkResponse(
        Guid Id,
        string Name,
        string Description,
        string Location,
        int GuestLimit,
        int AvailableGuestCapacity,
        decimal PricePerGuestPerDay,
        string Currency,
        IReadOnlyCollection<string> AvailableDates,
        DateTime CreatedAtUtc,
        DateTime LastModifiedUtc)
    {
        public static ParkResponse FromDomain(Park park)
        {
            var availableDates = park.AvailableDates
                .Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .ToList()
                .AsReadOnly();

            return new ParkResponse(
                park.Id,
                park.Name,
                park.Description,
                park.Location,
                park.GuestLimit,
                park.AvailableGuestCapacity,
                park.PricePerGuestPerDay.Amount,
                park.PricePerGuestPerDay.Currency,
                availableDates,
                park.CreatedAtUtc,
                park.LastModifiedUtc);
        }
    }

    private sealed class CreateParkRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public int GuestLimit { get; init; }
        public decimal PricePerGuestPerDay { get; init; }
        public string Currency { get; init; } = "USD";
        public List<string> AvailableDates { get; init; } = new();

        public bool TryValidate(out Dictionary<string, string[]> errors, out IReadOnlyCollection<DateOnly> availability)
        {
            errors = new Dictionary<string, string[]>();
            var dates = new List<DateOnly>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors["Name"] = new[] { "Name is required." };
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                errors["Description"] = new[] { "Description is required." };
            }

            if (string.IsNullOrWhiteSpace(Location))
            {
                errors["Location"] = new[] { "Location is required." };
            }

            if (GuestLimit <= 0)
            {
                errors["GuestLimit"] = new[] { "Guest limit must be greater than zero." };
            }

            if (PricePerGuestPerDay <= 0)
            {
                errors["PricePerGuestPerDay"] = new[] { "Price must be greater than zero." };
            }

            foreach (var value in AvailableDates)
            {
                if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    errors["AvailableDates"] = new[] { $"'{value}' is not a valid date (expected yyyy-MM-dd)." };
                    break;
                }

                dates.Add(date);
            }

            availability = dates.AsReadOnly();
            return errors.Count == 0;
        }

        public Park ToDomain(IReadOnlyCollection<DateOnly> availability)
        {
            return new Park(
                Guid.NewGuid(),
                Normalize(Name),
                Normalize(Description),
                Normalize(Location),
                GuestLimit,
                new Money(PricePerGuestPerDay, NormalizeCurrency(Currency)),
                availability);
        }
    }

    private sealed class UpdateParkRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public int GuestLimit { get; init; }
        public int? AvailableGuestCapacity { get; init; }
        public decimal PricePerGuestPerDay { get; init; }
        public string Currency { get; init; } = "USD";
        public List<string> AvailableDates { get; init; } = new();

        public bool TryApply(Park existing, out Park updated, out Dictionary<string, string[]> errors)
        {
            errors = new Dictionary<string, string[]>();
            updated = existing;

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors["Name"] = new[] { "Name is required." };
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                errors["Description"] = new[] { "Description is required." };
            }

            if (string.IsNullOrWhiteSpace(Location))
            {
                errors["Location"] = new[] { "Location is required." };
            }

            if (GuestLimit <= 0)
            {
                errors["GuestLimit"] = new[] { "Guest limit must be greater than zero." };
            }

            if (PricePerGuestPerDay <= 0)
            {
                errors["PricePerGuestPerDay"] = new[] { "Price must be greater than zero." };
            }

            var dates = new List<DateOnly>();
            foreach (var value in AvailableDates)
            {
                if (!DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    errors["AvailableDates"] = new[] { $"'{value}' is not a valid date (expected yyyy-MM-dd)." };
                    break;
                }

                dates.Add(date);
            }

            if (errors.Count > 0)
            {
                return false;
            }

            var normalizedCapacity = AvailableGuestCapacity ?? existing.AvailableGuestCapacity;
            normalizedCapacity = Math.Clamp(normalizedCapacity, 0, GuestLimit);

            var availability = dates.Count > 0 ? dates : existing.AvailableDates.ToList();

            updated = new Park(
                existing.Id,
                Normalize(Name),
                Normalize(Description),
                Normalize(Location),
                GuestLimit,
                normalizedCapacity,
                new Money(PricePerGuestPerDay, NormalizeCurrency(Currency)),
                availability,
                existing.CreatedAtUtc,
                DateTime.UtcNow);

            return true;
        }
    }
}
