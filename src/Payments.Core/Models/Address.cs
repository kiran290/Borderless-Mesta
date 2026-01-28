namespace Payments.Core.Models;

/// <summary>
/// Represents a physical address.
/// </summary>
public sealed class Address
{
    /// <summary>
    /// Street address line 1.
    /// </summary>
    public required string Street1 { get; init; }

    /// <summary>
    /// Street address line 2 (optional).
    /// </summary>
    public string? Street2 { get; init; }

    /// <summary>
    /// City name.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// State or province.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Postal or ZIP code.
    /// </summary>
    public required string PostalCode { get; init; }

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2).
    /// </summary>
    public required string CountryCode { get; init; }
}
