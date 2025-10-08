using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Services;

/// <summary>
/// Manages park operations such as listing, retrieval, and creation.
/// </summary>
public class ParkService : IParkAdminService
{
    private readonly IParkRepository _parkRepository;

    public ParkService(IParkRepository parkRepository)
    {
        _parkRepository = parkRepository;
    }

    public Task<IReadOnlyCollection<Park>> GetParksAsync(CancellationToken cancellationToken = default)
        => _parkRepository.GetAllAsync(cancellationToken);

    public Task<Park?> GetParkAsync(Guid id, CancellationToken cancellationToken = default)
        => _parkRepository.GetByIdAsync(id, cancellationToken);

    public Task AddParkAsync(Park park, CancellationToken cancellationToken = default)
        => _parkRepository.AddAsync(park, cancellationToken);

    public Task<bool> RemoveParkAsync(Guid id, CancellationToken cancellationToken = default)
        => _parkRepository.RemoveAsync(id, cancellationToken);

    public async Task<bool> AddGuestCapacityAsync(Guid parkId, int guestsToAdd, CancellationToken cancellationToken = default)
    {
        var park = await _parkRepository.GetByIdAsync(parkId, cancellationToken).ConfigureAwait(false);
        if (park is null)
        {
            return false;
        }

        park.UpdateGuestLimit(park.GuestLimit + guestsToAdd);
        await _parkRepository.UpdateAsync(park, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemoveGuestCapacityAsync(Guid parkId, int guestsToRemove, CancellationToken cancellationToken = default)
    {
        var park = await _parkRepository.GetByIdAsync(parkId, cancellationToken).ConfigureAwait(false);
        if (park is null)
        {
            return false;
        }

        try
        {
            park.UpdateGuestLimit(park.GuestLimit - guestsToRemove);
            await _parkRepository.UpdateAsync(park, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<bool> UpdateParkPricingAsync(Guid parkId, decimal newPricePerGuestPerDay, CancellationToken cancellationToken = default)
    {
        var park = await _parkRepository.GetByIdAsync(parkId, cancellationToken).ConfigureAwait(false);
        if (park is null)
        {
            return false;
        }

        park.UpdatePrice(new Money(newPricePerGuestPerDay));
        await _parkRepository.UpdateAsync(park, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
