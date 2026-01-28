namespace Payments.Core.Models.Responses;

/// <summary>
/// Result of a payout status query.
/// </summary>
public sealed class PayoutStatusResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The payout status update (if successful).
    /// </summary>
    public PayoutStatusUpdate? StatusUpdate { get; init; }

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
    public static PayoutStatusResult Succeeded(PayoutStatusUpdate statusUpdate) => new()
    {
        Success = true,
        StatusUpdate = statusUpdate
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static PayoutStatusResult Failed(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
