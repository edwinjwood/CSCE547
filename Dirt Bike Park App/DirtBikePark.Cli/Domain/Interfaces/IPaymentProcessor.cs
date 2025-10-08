using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;

namespace DirtBikePark.Cli.Domain.Interfaces;

/// <summary>
/// Provides a gateway for processing payments.
/// </summary>
public interface IPaymentProcessor
{
    Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a payment processing attempt.
/// </summary>
public sealed record PaymentResult(bool Success, string Message);
