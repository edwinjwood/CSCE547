using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;

namespace DirtBikePark.Cli.Domain.Interfaces;

/// <summary>
/// Abstraction for data operations related to <see cref="Booking"/> entities.
/// </summary>
public interface IBookingRepository
{
    Task<IReadOnlyCollection<Booking>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Booking>> GetByParkAsync(Guid parkId, CancellationToken cancellationToken = default);
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
