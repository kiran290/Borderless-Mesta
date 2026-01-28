namespace Payments.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Borderless API.
/// </summary>
public sealed class BorderlessSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Payout:Borderless";

    /// <summary>
    /// Base URL for the Borderless API.
    /// Production: https://api.borderless.xyz/v1
    /// Staging: https://api.sandbox.borderless.xyz/v1
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// API key for authentication.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// API secret for authentication.
    /// </summary>
    public required string ApiSecret { get; init; }

    /// <summary>
    /// Client/Organization ID assigned by Borderless.
    /// </summary>
    public required string ClientId { get; init; }

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
