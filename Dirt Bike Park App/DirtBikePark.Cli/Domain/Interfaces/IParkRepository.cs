using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;

namespace DirtBikePark.Cli.Domain.Interfaces;

/// <summary>
/// Abstraction for data operations related to <see cref="Park"/> entities.
/// </summary>
public interface IParkRepository
{
    Task<IReadOnlyCollection<Park>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Park?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Park park, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(Park park, CancellationToken cancellationToken = default);
}
