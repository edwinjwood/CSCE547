using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using DirtBikePark.Cli.Domain.Entities;
using DirtBikePark.Cli.Domain.Interfaces;
using DirtBikePark.Cli.Domain.ValueObjects;

namespace DirtBikePark.Cli.Domain.Services;

/// <summary>
/// Coordinates payment validation and processing through a payment processor.
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
        ValidateCardNumber(cardNumber);
        ValidateExpiration(expiration);
        ValidateCvc(cvc);

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

    //Validates the credit card number. Ensure it has the correct number of digits and passes the Luhn algorithm.
    private static void ValidateCardNumber(string cardNumber)
    {
        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 13 || digits.Length > 19)
        {
            throw new ArgumentException("Card number must be between 13 and 19 digits.", nameof(cardNumber));
        }

        if (!PassesLuhnCheck(digits))
        {
            throw new ArgumentException("Card number failed validation.", nameof(cardNumber));
        }
    }

    //Validates the expiration date. Ensure it is in MM/yy format and not expired.
    private static void ValidateExpiration(string expiration)
    {
        //Get the expiration date in MM/yy format and throw an exception if invalid
        if (!DateTime.TryParseExact(expiration, "MM/yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exp))
        {
            throw new ArgumentException("Expiration must be in MM/yy format.", nameof(expiration));
        }
        //Set the expiration date to the last day of the month at 23:59:59
        var lastDay = DateTime.DaysInMonth(exp.Year, exp.Month);
        var expirationDate = new DateTime(exp.Year, exp.Month, lastDay, 23, 59, 59);
        //Using this expiration date, check if the card is expired by comparing it to the current date
        if (expirationDate < DateTime.UtcNow)
        {
            throw new ArgumentException("Card has expired.", nameof(expiration));
        }
    }

    //Validates the CVC code. Ensure it is 3 or 4 digits.
    private static void ValidateCvc(string cvc)
    {
        if (cvc.Length is not (3 or 4) || !cvc.All(char.IsDigit))
        {
            throw new ArgumentException("CVC must be 3 or 4 digits.", nameof(cvc));
        }
    }
    //Implements the Luhn algorithm to validate the credit card number. 
    private static bool PassesLuhnCheck(string digits)
    {
        var sum = 0;
        var doubleDigit = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (doubleDigit)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }

            sum += d;
            doubleDigit = !doubleDigit;
        }

        return sum % 10 == 0;
    }
}
