using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;

namespace DirtBikePark.Cli.Domain.Interfaces;

/// <summary>
/// Abstraction for persisting <see cref="Cart"/> instances.
/// </summary>
public interface ICartRepository
{
    Task<Cart> GetOrCreateAsync(Guid? id = null, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Cart>> GetAllAsync(CancellationToken cancellationToken = default);
}
