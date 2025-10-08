using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;

namespace DirtBikePark.Cli.Infrastructure.Payments;

/// <summary>
/// Mock payment processor that simulates success for valid requests.
/// </summary>
public class MockPaymentProcessor : IPaymentProcessor
{
    public Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Simple deterministic success rule: last digit of card number determines success
        var digits = new string(request.CardNumber.Where(char.IsDigit).ToArray());
        var success = digits.Length > 0 && (digits.Last() - '0') % 2 == 0;
        var message = success ? "Payment processed successfully." : "Payment declined by issuer.";
        return Task.FromResult(new PaymentResult(success, message));
    }
}
