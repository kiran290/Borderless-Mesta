namespace Payments.Core.Models.Responses;

/// <summary>
/// Result of a payout operation.
/// </summary>
public sealed class PayoutResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The created payout (if successful).
    /// </summary>
    public Payout? Payout { get; init; }

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
    public static PayoutResult Succeeded(Payout payout) => new()
    {
        Success = true,
        Payout = payout
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static PayoutResult Failed(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
