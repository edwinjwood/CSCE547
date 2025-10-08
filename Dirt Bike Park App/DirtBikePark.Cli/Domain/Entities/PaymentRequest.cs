using System;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Entities;

/// <summary>
/// Represents the information required to process a payment.
/// </summary>
public class PaymentRequest
{
    public Guid CartId { get; }
    public string CardholderName { get; }
    public string CardNumber { get; }
    public string ExpirationMonthYear { get; }
    public string Cvc { get; }
    public Address BillingAddress { get; }
    public Money Amount { get; }

    public PaymentRequest(
        Guid cartId,
        string cardholderName,
        string cardNumber,
        string expirationMonthYear,
        string cvc,
        Address billingAddress,
        Money amount)
    {
        CartId = cartId;
        CardholderName = string.IsNullOrWhiteSpace(cardholderName)
            ? throw new ArgumentException("Cardholder name is required.", nameof(cardholderName))
            : cardholderName.Trim();
        CardNumber = cardNumber?.Replace(" ", string.Empty) ?? throw new ArgumentNullException(nameof(cardNumber));
        ExpirationMonthYear = expirationMonthYear ?? throw new ArgumentNullException(nameof(expirationMonthYear));
        Cvc = cvc ?? throw new ArgumentNullException(nameof(cvc));
        BillingAddress = billingAddress ?? throw new ArgumentNullException(nameof(billingAddress));
        Amount = amount;
    }
}
