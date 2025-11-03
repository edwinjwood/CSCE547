using System;

namespace DirtBikePark.Cli.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value in a specific currency.
/// </summary>
public readonly struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    //Constructor for Money. Assumes currency is USD if not specified.
    public Money(decimal amount, string currency = "USD")
    {
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }
    // Add money
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }
    // Subtract money
    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }
    // Multiply money by a multiplier
    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    //Override ToString method to format currency and amount.
    public override string ToString() => $"{Currency} {Amount:N2}";

    //Ensures both Money instances are in the same currency before performing operations.
    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!left.Currency.Equals(right.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot operate on Money values with different currencies.");
        }
    }
}
