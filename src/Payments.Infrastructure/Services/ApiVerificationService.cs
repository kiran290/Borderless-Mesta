using Microsoft.Extensions.Logging;
using Payments.Core.Enums;
using Payments.Core.Interfaces;
using Payments.Core.Models;
using Payments.Core.Models.Requests;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Service for verifying API connectivity and testing provider operations.
/// Use this to validate that providers are configured correctly.
/// </summary>
public sealed class ApiVerificationService
{
    private readonly PaymentProviderFactory _providerFactory;
    private readonly ILogger<ApiVerificationService> _logger;

    public ApiVerificationService(
        PaymentProviderFactory providerFactory,
        ILogger<ApiVerificationService> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Verifies all configured providers.
    /// </summary>
    public async Task<ProviderVerificationReport> VerifyAllProvidersAsync(
        CancellationToken cancellationToken = default)
    {
        var report = new ProviderVerificationReport
        {
            StartTime = DateTime.UtcNow
        };

        var providers = _providerFactory.GetAllProviders().ToList();
        report.TotalProviders = providers.Count;

        foreach (var provider in providers)
        {
            var providerReport = await VerifyProviderAsync(provider, cancellationToken);
            report.ProviderReports.Add(providerReport);

            if (providerReport.OverallSuccess)
            {
                report.SuccessfulProviders++;
            }
        }

        report.EndTime = DateTime.UtcNow;
        return report;
    }

    /// <summary>
    /// Verifies a specific provider by ID.
    /// </summary>
    public async Task<SingleProviderReport> VerifyProviderAsync(
        PayoutProvider providerId,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.GetProvider(providerId);
        if (provider == null)
        {
            return new SingleProviderReport
            {
                ProviderId = providerId,
                ProviderName = providerId.ToString(),
                OverallSuccess = false,
                HealthCheck = new OperationResult
                {
                    Operation = "Health Check",
                    Success = false,
                    Error = "Provider not configured"
                }
            };
        }

        return await VerifyProviderAsync(provider, cancellationToken);
    }

    private async Task<SingleProviderReport> VerifyProviderAsync(
        IPaymentProvider provider,
        CancellationToken cancellationToken)
    {
        var report = new SingleProviderReport
        {
            ProviderId = provider.ProviderId,
            ProviderName = provider.ProviderName
        };

        _logger.LogInformation("Starting verification for provider: {Provider}", provider.ProviderName);

        // 1. Health Check
        report.HealthCheck = await VerifyHealthCheckAsync(provider, cancellationToken);

        // If health check fails, skip other tests
        if (!report.HealthCheck.Success)
        {
            _logger.LogWarning("Provider {Provider} health check failed, skipping other tests", provider.ProviderName);
            return report;
        }

        // 2. Test Customer Creation (dry run - just test API connectivity)
        report.CustomerCreation = await VerifyCustomerCreationAsync(provider, cancellationToken);

        // 3. Test Quote Creation (dry run)
        report.QuoteCreation = await VerifyQuoteCreationAsync(provider, cancellationToken);

        report.OverallSuccess = report.HealthCheck.Success &&
                                (report.CustomerCreation?.Success ?? false) &&
                                (report.QuoteCreation?.Success ?? false);

        _logger.LogInformation(
            "Verification completed for provider {Provider}: Success={Success}",
            provider.ProviderName,
            report.OverallSuccess);

        return report;
    }

    private async Task<OperationResult> VerifyHealthCheckAsync(
        IPaymentProvider provider,
        CancellationToken cancellationToken)
    {
        var result = new OperationResult { Operation = "Health Check" };

        try
        {
            var health = await provider.CheckHealthAsync(cancellationToken);
            result.Success = health.IsHealthy;
            result.Latency = health.Latency;
            result.Message = health.Message;

            if (!health.IsHealthy)
            {
                result.Error = health.Message ?? "Provider is not healthy";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError(ex, "Health check failed for {Provider}", provider.ProviderName);
        }

        return result;
    }

    private async Task<OperationResult> VerifyCustomerCreationAsync(
        IPaymentProvider provider,
        CancellationToken cancellationToken)
    {
        var result = new OperationResult { Operation = "Customer Creation" };

        try
        {
            // Create a test customer (this will hit the actual API)
            var request = new CreateCustomerRequest
            {
                Type = CustomerType.Individual,
                Role = CustomerRole.Sender,
                Individual = new IndividualDetails
                {
                    FirstName = "API",
                    LastName = "Test",
                    DateOfBirth = new DateOnly(1990, 1, 1)
                },
                Contact = new ContactInfo
                {
                    Email = $"apitest+{Guid.NewGuid():N}@test.local",
                    Phone = "+15551234567"
                },
                Address = new Address
                {
                    Street1 = "123 Test Street",
                    City = "Test City",
                    State = "CA",
                    PostalCode = "90210",
                    CountryCode = "US"
                },
                ExternalId = $"test_{Guid.NewGuid():N}"
            };

            var customerResult = await provider.CreateCustomerAsync(request, cancellationToken);

            result.Success = customerResult.Success;
            result.Message = customerResult.Success
                ? $"Customer created: {customerResult.Customer?.Id}"
                : customerResult.ErrorMessage;

            if (!customerResult.Success)
            {
                result.Error = $"{customerResult.ErrorCode}: {customerResult.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError(ex, "Customer creation test failed for {Provider}", provider.ProviderName);
        }

        return result;
    }

    private async Task<OperationResult> VerifyQuoteCreationAsync(
        IPaymentProvider provider,
        CancellationToken cancellationToken)
    {
        var result = new OperationResult { Operation = "Quote Creation" };

        try
        {
            var request = new CreateQuoteRequest
            {
                SourceCurrency = Stablecoin.USDC,
                TargetCurrency = FiatCurrency.USD,
                SourceAmount = 100.00m,
                Network = BlockchainNetwork.Ethereum,
                DestinationCountry = "US",
                PaymentMethod = PaymentMethod.Ach
            };

            var quoteResult = await provider.CreateQuoteAsync(request, cancellationToken);

            result.Success = quoteResult.Success;
            result.Message = quoteResult.Success
                ? $"Quote created: Rate={quoteResult.Quote?.ExchangeRate}, Fee={quoteResult.Quote?.FeeAmount}"
                : quoteResult.ErrorMessage;

            if (!quoteResult.Success)
            {
                result.Error = $"{quoteResult.ErrorCode}: {quoteResult.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            _logger.LogError(ex, "Quote creation test failed for {Provider}", provider.ProviderName);
        }

        return result;
    }
}

/// <summary>
/// Overall verification report for all providers.
/// </summary>
public sealed class ProviderVerificationReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public int TotalProviders { get; set; }
    public int SuccessfulProviders { get; set; }
    public bool AllProvidersHealthy => TotalProviders == SuccessfulProviders;
    public List<SingleProviderReport> ProviderReports { get; } = [];
}

/// <summary>
/// Verification report for a single provider.
/// </summary>
public sealed class SingleProviderReport
{
    public PayoutProvider ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool OverallSuccess { get; set; }
    public OperationResult? HealthCheck { get; set; }
    public OperationResult? CustomerCreation { get; set; }
    public OperationResult? QuoteCreation { get; set; }
    public OperationResult? KycInitiation { get; set; }
    public OperationResult? PayoutCreation { get; set; }
}

/// <summary>
/// Result of a single operation test.
/// </summary>
public sealed class OperationResult
{
    public string Operation { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public TimeSpan? Latency { get; set; }
}
