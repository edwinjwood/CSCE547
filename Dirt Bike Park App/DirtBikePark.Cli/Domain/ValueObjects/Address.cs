using System;
using System.Text.RegularExpressions;

namespace DirtBikePark.Cli.Domain.ValueObjects;

/// <summary>
/// Represents a postal address used for billing or contact purposes.
/// </summary>
public sealed class Address
{
    //Regex pattern to ensure valid street format (ex. "301 Hill St")
    private static readonly Regex StreetPattern = new("^[0-9]+\\s+.+", RegexOptions.Compiled);

    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }

    //Constructor for address, ensure each field is valid
    public Address(string street, string city, string state, string postalCode)
    {
        Street = ValidateStreet(street);
        City = ValidateRequired(city, nameof(city));
        State = ValidateRequired(state, nameof(state));
        PostalCode = ValidateRequired(postalCode, nameof(postalCode));
    }

    //Validate the street format using the regex StreetPattern. 
    private static string ValidateStreet(string value)
    {
        var trimmed = ValidateRequired(value, nameof(Street));
        if (!StreetPattern.IsMatch(trimmed))
        {
            throw new ArgumentException("Street must include a number and street name.", nameof(value));
        }

        return trimmed;
    }
    //For the other fields, ensure they are not null or empty after trimming whitespace.
    private static string ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} cannot be null or empty.", fieldName);
        }

        return value.Trim();
    }

    //Override ToString method to format the address
    public override string ToString() => $"{Street}, {City}, {State} {PostalCode}";
}
