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

    //Get all parks in the system.
    public Task<IReadOnlyCollection<Park>> GetParksAsync(CancellationToken cancellationToken = default)
        => _parkRepository.GetAllAsync(cancellationToken);

    //Get a specific park by its id.
    public Task<Park?> GetParkAsync(Guid id, CancellationToken cancellationToken = default)
        => _parkRepository.GetByIdAsync(id, cancellationToken);

    //Add a new park to the system.
    public Task AddParkAsync(Park park, CancellationToken cancellationToken = default)
        => _parkRepository.AddAsync(park, cancellationToken);

    //Remove a park from the system by its id.
    public Task<bool> RemoveParkAsync(Guid id, CancellationToken cancellationToken = default)
        => _parkRepository.RemoveAsync(id, cancellationToken);

    //Add guest capacity to an existing park by its id. Guest limit is increased by the original guest limit plus the specified number of guests to add.
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

    //Remove guest capacity from an existing park by its id. Guest limit is decreased by the original guest limit minus the specified number of guests to remove.
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
    //Update the pricing of an existing park by its id.
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
