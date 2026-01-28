using Payments.Core.Enums;

namespace Payments.Infrastructure.Configuration;

/// <summary>
/// General payout configuration settings.
/// </summary>
public sealed class PayoutSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Payout";

    /// <summary>
    /// Default payout provider to use.
    /// </summary>
    public PayoutProvider DefaultProvider { get; init; } = PayoutProvider.Mesta;

    /// <summary>
    /// Enable automatic provider failover.
    /// </summary>
    public bool EnableFailover { get; init; } = true;

    /// <summary>
    /// Quote validity period in minutes.
    /// </summary>
    public int QuoteValidityMinutes { get; init; } = 5;

    /// <summary>
    /// Maximum retry attempts for provider operations.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Mesta provider settings.
    /// </summary>
    public MestaSettings? Mesta { get; init; }

    /// <summary>
    /// Borderless provider settings.
    /// </summary>
    public BorderlessSettings? Borderless { get; init; }
}
