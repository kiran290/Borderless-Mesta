using Payments.Core.Enums;

namespace Payments.Core.Exceptions;

/// <summary>
/// Base exception for payout-related errors.
/// </summary>
public class PayoutException : Exception
{
    /// <summary>
    /// Error code for categorizing the exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Provider that caused the exception (if applicable).
    /// </summary>
    public PayoutProvider? Provider { get; }

    /// <summary>
    /// Creates a new payout exception.
    /// </summary>
    public PayoutException(string errorCode, string message, PayoutProvider? provider = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Provider = provider;
    }
}

/// <summary>
/// Exception thrown when a provider is not available.
/// </summary>
public class ProviderUnavailableException : PayoutException
{
    public ProviderUnavailableException(PayoutProvider provider, string message)
        : base("PROVIDER_UNAVAILABLE", message, provider)
    {
    }
}

/// <summary>
/// Exception thrown when a provider is not found or not registered.
/// </summary>
public class ProviderNotFoundException : PayoutException
{
    public ProviderNotFoundException(PayoutProvider provider)
        : base("PROVIDER_NOT_FOUND", $"Provider '{provider}' is not registered or configured.", provider)
    {
    }
}

/// <summary>
/// Exception thrown when the requested configuration is not supported.
/// </summary>
public class UnsupportedConfigurationException : PayoutException
{
    public Stablecoin SourceCurrency { get; }
    public FiatCurrency TargetCurrency { get; }
    public BlockchainNetwork Network { get; }
    public string DestinationCountry { get; }

    public UnsupportedConfigurationException(
        Stablecoin sourceCurrency,
        FiatCurrency targetCurrency,
        BlockchainNetwork network,
        string destinationCountry,
        PayoutProvider? provider = null)
        : base("UNSUPPORTED_CONFIGURATION",
            $"The configuration ({sourceCurrency} -> {targetCurrency} via {network} to {destinationCountry}) is not supported.",
            provider)
    {
        SourceCurrency = sourceCurrency;
        TargetCurrency = targetCurrency;
        Network = network;
        DestinationCountry = destinationCountry;
    }
}

/// <summary>
/// Exception thrown when a quote has expired.
/// </summary>
public class QuoteExpiredException : PayoutException
{
    public string QuoteId { get; }

    public QuoteExpiredException(string quoteId, PayoutProvider? provider = null)
        : base("QUOTE_EXPIRED", $"Quote '{quoteId}' has expired.", provider)
    {
        QuoteId = quoteId;
    }
}

/// <summary>
/// Exception thrown when a payout is not found.
/// </summary>
public class PayoutNotFoundException : PayoutException
{
    public string PayoutId { get; }

    public PayoutNotFoundException(string payoutId, PayoutProvider? provider = null)
        : base("PAYOUT_NOT_FOUND", $"Payout '{payoutId}' was not found.", provider)
    {
        PayoutId = payoutId;
    }
}

/// <summary>
/// Exception thrown when there is an authentication error with a provider.
/// </summary>
public class ProviderAuthenticationException : PayoutException
{
    public ProviderAuthenticationException(PayoutProvider provider, string message)
        : base("PROVIDER_AUTH_ERROR", message, provider)
    {
    }
}

/// <summary>
/// Exception thrown when a provider returns an error.
/// </summary>
public class ProviderApiException : PayoutException
{
    public int? HttpStatusCode { get; }
    public string? ProviderErrorCode { get; }
    public string? ProviderErrorMessage { get; }

    public ProviderApiException(
        PayoutProvider provider,
        string message,
        int? httpStatusCode = null,
        string? providerErrorCode = null,
        string? providerErrorMessage = null,
        Exception? innerException = null)
        : base("PROVIDER_API_ERROR", message, provider, innerException)
    {
        HttpStatusCode = httpStatusCode;
        ProviderErrorCode = providerErrorCode;
        ProviderErrorMessage = providerErrorMessage;
    }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class PayoutValidationException : PayoutException
{
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    public PayoutValidationException(IDictionary<string, string[]> validationErrors)
        : base("VALIDATION_ERROR", "One or more validation errors occurred.")
    {
        ValidationErrors = new Dictionary<string, string[]>(validationErrors);
    }

    public PayoutValidationException(string field, string error)
        : base("VALIDATION_ERROR", $"Validation error on field '{field}': {error}")
    {
        ValidationErrors = new Dictionary<string, string[]>
        {
            { field, new[] { error } }
        };
    }
}

/// <summary>
/// Exception thrown when a payout cannot be cancelled.
/// </summary>
public class PayoutCancellationException : PayoutException
{
    public string PayoutId { get; }
    public PayoutStatus CurrentStatus { get; }

    public PayoutCancellationException(string payoutId, PayoutStatus currentStatus, PayoutProvider? provider = null)
        : base("PAYOUT_CANCELLATION_FAILED",
            $"Payout '{payoutId}' cannot be cancelled in status '{currentStatus}'.",
            provider)
    {
        PayoutId = payoutId;
        CurrentStatus = currentStatus;
    }
}
