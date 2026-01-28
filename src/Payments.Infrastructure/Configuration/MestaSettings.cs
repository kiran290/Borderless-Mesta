namespace Payments.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Mesta API.
/// </summary>
public sealed class MestaSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Payout:Mesta";

    /// <summary>
    /// Base URL for the Mesta API.
    /// Production: https://api.mesta.xyz/v1
    /// Staging: https://api.stg.mesta.xyz/v1
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// API key for authentication.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// API secret for authentication (if required).
    /// </summary>
    public string? ApiSecret { get; init; }

    /// <summary>
    /// Merchant ID assigned by Mesta.
    /// </summary>
    public required string MerchantId { get; init; }

    /// <summary>
    /// Webhook secret for validating incoming webhooks.
    /// </summary>
    public string? WebhookSecret { get; init; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Number of retry attempts for failed requests.
    /// </summary>
    public int RetryAttempts { get; init; } = 3;

    /// <summary>
    /// Indicates if this provider is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;
}
