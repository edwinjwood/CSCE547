using System;
using System.Threading;
using System.Threading.Tasks;

namespace DirtBikePark.Cli.Domain.Interfaces;

/// <summary>
/// Administrative operations for managing park capacity and pricing.
/// </summary>
public interface IParkAdminService
{
    Task<bool> AddGuestCapacityAsync(Guid parkId, int guestsToAdd, CancellationToken cancellationToken = default);
    Task<bool> RemoveGuestCapacityAsync(Guid parkId, int guestsToRemove, CancellationToken cancellationToken = default);
    Task<bool> UpdateParkPricingAsync(Guid parkId, decimal newPricePerGuestPerDay, CancellationToken cancellationToken = default);
}
