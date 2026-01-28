using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Core.Enums;
using Payments.Core.Interfaces;

namespace Payments.Api.Controllers;

/// <summary>
/// Controller for provider information and management.
/// </summary>
[ApiController]
[Route("api/v1/providers")]
[Produces("application/json")]
public sealed class ProvidersController : ControllerBase
{
    private readonly IPayoutProviderFactory _providerFactory;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(
        IPayoutProviderFactory providerFactory,
        ILogger<ProvidersController> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available payout providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available providers with their capabilities.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProviderInfoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Getting all providers [RequestId: {RequestId}]", requestId);

        var providers = _providerFactory.GetAllProviders();

        var providerInfos = new List<ProviderInfoDto>();
        foreach (var provider in providers)
        {
            var isAvailable = await provider.IsAvailableAsync(cancellationToken);
            providerInfos.Add(new ProviderInfoDto
            {
                Id = provider.ProviderId,
                Name = provider.ProviderName,
                SupportedStablecoins = provider.SupportedStablecoins,
                SupportedFiatCurrencies = provider.SupportedFiatCurrencies,
                SupportedNetworks = provider.SupportedNetworks,
                IsAvailable = isAvailable
            });
        }

        return Ok(ApiResponse<IReadOnlyList<ProviderInfoDto>>.Ok(providerInfos, requestId));
    }

    /// <summary>
    /// Gets a specific provider by ID.
    /// </summary>
    /// <param name="id">Provider ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provider information.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvider(PayoutProvider id, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Getting provider: {ProviderId} [RequestId: {RequestId}]", id, requestId);

        try
        {
            var provider = _providerFactory.GetProvider(id);
            var isAvailable = await provider.IsAvailableAsync(cancellationToken);

            var providerInfo = new ProviderInfoDto
            {
                Id = provider.ProviderId,
                Name = provider.ProviderName,
                SupportedStablecoins = provider.SupportedStablecoins,
                SupportedFiatCurrencies = provider.SupportedFiatCurrencies,
                SupportedNetworks = provider.SupportedNetworks,
                IsAvailable = isAvailable
            };

            return Ok(ApiResponse<ProviderInfoDto>.Ok(providerInfo, requestId));
        }
        catch
        {
            return NotFound(ApiResponse<object>.Fail(
                "PROVIDER_NOT_FOUND",
                $"Provider '{id}' was not found",
                requestId));
        }
    }

    /// <summary>
    /// Gets providers that support a specific configuration.
    /// </summary>
    /// <param name="sourceCurrency">Source stablecoin.</param>
    /// <param name="targetCurrency">Target fiat currency.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="destinationCountry">Destination country code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of providers supporting the configuration.</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProviderInfoDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProviders(
        [FromQuery] Stablecoin sourceCurrency,
        [FromQuery] FiatCurrency targetCurrency,
        [FromQuery] BlockchainNetwork network,
        [FromQuery] string destinationCountry,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Searching providers: {SourceCurrency} -> {TargetCurrency} via {Network} to {Country} [RequestId: {RequestId}]",
            sourceCurrency,
            targetCurrency,
            network,
            destinationCountry,
            requestId);

        var providers = _providerFactory.GetSupportingProviders(
            sourceCurrency,
            targetCurrency,
            network,
            destinationCountry);

        var providerInfos = new List<ProviderInfoDto>();
        foreach (var provider in providers)
        {
            var isAvailable = await provider.IsAvailableAsync(cancellationToken);
            providerInfos.Add(new ProviderInfoDto
            {
                Id = provider.ProviderId,
                Name = provider.ProviderName,
                SupportedStablecoins = provider.SupportedStablecoins,
                SupportedFiatCurrencies = provider.SupportedFiatCurrencies,
                SupportedNetworks = provider.SupportedNetworks,
                IsAvailable = isAvailable
            });
        }

        return Ok(ApiResponse<IReadOnlyList<ProviderInfoDto>>.Ok(providerInfos, requestId));
    }

    /// <summary>
    /// Checks the health/availability of a specific provider.
    /// </summary>
    /// <param name="id">Provider ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health status.</returns>
    [HttpGet("{id}/health")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckProviderHealth(PayoutProvider id, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Checking provider health: {ProviderId} [RequestId: {RequestId}]", id, requestId);

        try
        {
            var provider = _providerFactory.GetProvider(id);
            var isAvailable = await provider.IsAvailableAsync(cancellationToken);

            return Ok(ApiResponse<bool>.Ok(isAvailable, requestId));
        }
        catch
        {
            return NotFound(ApiResponse<object>.Fail(
                "PROVIDER_NOT_FOUND",
                $"Provider '{id}' was not found",
                requestId));
        }
    }
}
