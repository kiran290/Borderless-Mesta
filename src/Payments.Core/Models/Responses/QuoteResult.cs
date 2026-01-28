namespace Payments.Core.Models.Responses;

/// <summary>
/// Result of a quote operation.
/// </summary>
public sealed class QuoteResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The created quote (if successful).
    /// </summary>
    public PayoutQuote? Quote { get; init; }

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
    public static QuoteResult Succeeded(PayoutQuote quote) => new()
    {
        Success = true,
        Quote = quote
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static QuoteResult Failed(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
