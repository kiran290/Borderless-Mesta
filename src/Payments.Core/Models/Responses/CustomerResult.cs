namespace Payments.Core.Models.Responses;

/// <summary>
/// Result of a customer operation.
/// </summary>
public sealed class CustomerResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The customer (if successful).
    /// </summary>
    public Customer? Customer { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CustomerResult Succeeded(Customer customer) => new()
    {
        Success = true,
        Customer = customer
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static CustomerResult Failed(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
