using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;

namespace DirtBikePark.Cli.Infrastructure.Repositories;

/// <summary>
/// Simple in-memory cart repository supporting multiple carts per session.
/// </summary>
public class InMemoryCartRepository : ICartRepository
{
    private readonly Dictionary<Guid, Cart> _carts = new();

    public Task<Cart> GetOrCreateAsync(Guid? id = null, CancellationToken cancellationToken = default)
    {
        if (id.HasValue && id.Value != Guid.Empty && _carts.TryGetValue(id.Value, out var existing))
        {
            return Task.FromResult(Clone(existing));
        }

        var cartId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();
        if (!_carts.TryGetValue(cartId, out var cart))
        {
            cart = new Cart(cartId);
            _carts[cart.Id] = cart;
        }

        return Task.FromResult(Clone(cart));
    }

    public Task UpdateAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        _carts[cart.Id] = Clone(cart);
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_carts.Remove(id));
    }

    public Task<IReadOnlyCollection<Cart>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Cart> snapshot = _carts.Values.Select(Clone).ToList().AsReadOnly();
        return Task.FromResult(snapshot);
    }

    private static Cart Clone(Cart cart)
    {
        var newCart = new Cart(cart.Id);
        foreach (var item in cart.Items)
        {
            newCart.AddOrUpdateItem(new CartItem(item.BookingId, item.ParkName, item.Quantity, item.UnitPrice));
        }

        return newCart;
    }
}
