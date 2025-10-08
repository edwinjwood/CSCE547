using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;
using DirtBikePark.Cli.Infrastructure.Persistence.Entities;
using DirtBikePark.Cli.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace DirtBikePark.Cli.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed implementation of <see cref="ICartRepository"/> using Entity Framework Core.
/// </summary>
public sealed class SqliteCartRepository : ICartRepository
{
    private readonly DirtBikeParkDbContextFactory _contextFactory;

    public SqliteCartRepository(DirtBikeParkDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Cart> GetOrCreateAsync(Guid? id = null, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        CartRecord? record = null;
        if (id.HasValue && id.Value != Guid.Empty)
        {
            record = await context.Carts
                .AsNoTracking()
                .Include(c => c.Items)
                .SingleOrDefaultAsync(c => c.Id == id.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        if (record is null)
        {
            var cart = id.HasValue && id.Value != Guid.Empty ? new Cart(id) : new Cart();
            var recordToSave = MapToRecord(cart);
            context.Carts.Add(recordToSave);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return cart;
        }

        return MapToDomain(record);
    }

    public async Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existing = await context.Carts
            .Include(c => c.Items)
            .SingleOrDefaultAsync(c => c.Id == cart.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            context.Carts.Add(MapToRecord(cart));
        }
        else
        {
            existing.CreatedAtUtc = cart.CreatedAtUtc;
            existing.LastUpdatedUtc = cart.LastUpdatedUtc;

            existing.Items.Clear();
            foreach (var item in cart.Items)
            {
                existing.Items.Add(MapItemToRecord(cart.Id, item));
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existing = await context.Carts.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        context.Carts.Remove(existing);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyCollection<Cart>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var records = await context.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(MapToDomain).ToList().AsReadOnly();
    }

    private static Cart MapToDomain(CartRecord record)
    {
        var items = record.Items
            .Select(i => new CartItem(i.BookingId, i.ParkName, i.Quantity, new Money(i.UnitPriceAmount, i.UnitPriceCurrency)))
            .ToList();

        return new Cart(record.Id, record.CreatedAtUtc, record.LastUpdatedUtc, items);
    }

    private static CartRecord MapToRecord(Cart cart)
    {
        var record = new CartRecord
        {
            Id = cart.Id,
            CreatedAtUtc = cart.CreatedAtUtc,
            LastUpdatedUtc = cart.LastUpdatedUtc
        };

        foreach (var item in cart.Items)
        {
            record.Items.Add(MapItemToRecord(cart.Id, item));
        }

        return record;
    }

    private static CartItemRecord MapItemToRecord(Guid cartId, CartItem item)
    {
        return new CartItemRecord
        {
            CartId = cartId,
            BookingId = item.BookingId,
            ParkName = item.ParkName,
            Quantity = item.Quantity,
            UnitPriceAmount = item.UnitPrice.Amount,
            UnitPriceCurrency = item.UnitPrice.Currency
        };
    }
}
