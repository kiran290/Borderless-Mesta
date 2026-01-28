using Payments.Core.Enums;

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
    /// Provider that processed the request.
    /// </summary>
    public PayoutProvider? Provider { get; init; }

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
    public static PayoutResult Succeeded(Payout payout, PayoutProvider? provider = null) => new()
    {
        Success = true,
        Payout = payout,
        Provider = provider ?? payout.Provider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static PayoutResult Failed(string errorCode, string errorMessage, PayoutProvider? provider = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Provider = provider
    };
}

/// <summary>
/// Result of a payout list operation.
/// </summary>
public sealed class PayoutListResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// List of payouts.
    /// </summary>
    public IReadOnlyList<Payout> Payouts { get; init; } = [];

    /// <summary>
    /// Total count of payouts matching the filter.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Provider that processed the request.
    /// </summary>
    public PayoutProvider? Provider { get; init; }

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
    public static PayoutListResult Succeeded(
        IReadOnlyList<Payout> payouts,
        int totalCount,
        int page,
        int pageSize,
        PayoutProvider provider) => new()
    {
        Success = true,
        Payouts = payouts,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        Provider = provider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static PayoutListResult Failed(string errorCode, string errorMessage, PayoutProvider? provider = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Provider = provider
    };
}
