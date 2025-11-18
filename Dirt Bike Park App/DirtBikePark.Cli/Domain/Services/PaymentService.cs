using System;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Services;

/// <summary>
/// Coordinates payment processing through a payment processor.
/// </summary>
public class PaymentService
{
    private readonly IPaymentProcessor _paymentProcessor;
    private readonly CartService _cartService;

    public PaymentService(IPaymentProcessor paymentProcessor, CartService cartService)
    {
        _paymentProcessor = paymentProcessor;
        _cartService = cartService;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        Guid cartId,
        string cardholderName,
        string cardNumber,
        string expiration,
        string cvc,
        Address billingAddress,
        CancellationToken cancellationToken = default)
    {
        var totals = await _cartService.CalculateTotalsAsync(cartId, cancellationToken).ConfigureAwait(false);
        var request = new PaymentRequest(
            cartId,
            cardholderName,
            cardNumber,
            expiration,
            cvc,
            billingAddress,
            totals.TotalWithTax);

        return await _paymentProcessor.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
