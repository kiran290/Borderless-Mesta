using StablecoinPayments.Core.Enums;

namespace StablecoinPayments.Infrastructure.Configuration;

public sealed class PaymentSettings
{
    public PaymentProvider DefaultProvider { get; set; } = PaymentProvider.Mesta;
    public bool EnableFailover { get; set; } = true;
    public int QuoteValidityMinutes { get; set; } = 5;
    public int MaxRetryAttempts { get; set; } = 3;
    public MestaSettings Mesta { get; set; } = new();
    public BorderlessSettings Borderless { get; set; } = new();
}

public sealed class MestaSettings
{
    public string BaseUrl { get; set; } = "https://api.stg.mesta.xyz/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public bool Enabled { get; set; } = true;
}

public sealed class BorderlessSettings
{
    public string BaseUrl { get; set; } = "https://api.sandbox.borderless.xyz/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryAttempts { get; set; } = 3;
    public bool Enabled { get; set; } = true;
}
