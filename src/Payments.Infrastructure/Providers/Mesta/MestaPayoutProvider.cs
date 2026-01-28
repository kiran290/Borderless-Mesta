using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Core.Enums;
using Payments.Core.Exceptions;
using Payments.Core.Interfaces;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;
using Payments.Infrastructure.Configuration;

namespace Payments.Infrastructure.Providers.Mesta;

/// <summary>
/// Mesta payout provider implementation.
/// Implements stablecoin to fiat payouts via Mesta API.
/// </summary>
public sealed class MestaPayoutProvider : IPayoutProvider, IWebhookHandler
{
    private readonly HttpClient _httpClient;
    private readonly MestaSettings _settings;
    private readonly ILogger<MestaPayoutProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private static readonly IReadOnlyList<Stablecoin> _supportedStablecoins = new[]
    {
        Stablecoin.USDC,
        Stablecoin.USDT
    };

    private static readonly IReadOnlyList<FiatCurrency> _supportedFiatCurrencies = new[]
    {
        FiatCurrency.USD,
        FiatCurrency.EUR,
        FiatCurrency.GBP
    };

    private static readonly IReadOnlyList<BlockchainNetwork> _supportedNetworks = new[]
    {
        BlockchainNetwork.Ethereum,
        BlockchainNetwork.Polygon,
        BlockchainNetwork.Arbitrum,
        BlockchainNetwork.Base,
        BlockchainNetwork.Tron
    };

    private static readonly HashSet<string> _supportedCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "GB", "DE", "FR", "ES", "IT", "NL", "BE", "AT", "PT",
        "IE", "FI", "SE", "DK", "NO", "CH", "PL", "CZ", "HU", "RO",
        "BG", "HR", "SK", "SI", "LT", "LV", "EE", "MT", "CY", "LU",
        "GR", "MX", "BR", "PH", "IN", "SG", "AU", "NZ", "CA", "JP"
    };

    public PayoutProvider ProviderId => PayoutProvider.Mesta;
    public string ProviderName => "Mesta";
    public IReadOnlyList<Stablecoin> SupportedStablecoins => _supportedStablecoins;
    public IReadOnlyList<FiatCurrency> SupportedFiatCurrencies => _supportedFiatCurrencies;
    public IReadOnlyList<BlockchainNetwork> SupportedNetworks => _supportedNetworks;
    PayoutProvider IWebhookHandler.Provider => PayoutProvider.Mesta;

    public MestaPayoutProvider(
        HttpClient httpClient,
        IOptions<MestaSettings> settings,
        ILogger<MestaPayoutProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("X-Merchant-Id", _settings.MerchantId);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"merchants/{_settings.MerchantId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mesta provider availability check failed");
            return false;
        }
    }

    public async Task<Sender> CreateSenderAsync(Sender sender, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating sender in Mesta: {Email}", sender.Email);

        var request = MapToMestaSender(sender);
        var response = await PostAsync<MestaSenderResponse>("senders", request, cancellationToken);

        sender.Id = response.Id;
        _logger.LogInformation("Sender created in Mesta with ID: {SenderId}", response.Id);

        return sender;
    }

    public async Task<Beneficiary> CreateBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating beneficiary in Mesta: {Email}", beneficiary.Email);

        var request = MapToMestaBeneficiary(beneficiary);
        var response = await PostAsync<MestaBeneficiaryResponse>("beneficiaries", request, cancellationToken);

        beneficiary.Id = response.Id;
        _logger.LogInformation("Beneficiary created in Mesta with ID: {BeneficiaryId}", response.Id);

        return beneficiary;
    }

    public async Task<QuoteResult> GetQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting quote from Mesta: {SourceCurrency} -> {TargetCurrency}",
            request.SourceCurrency,
            request.TargetCurrency);

        try
        {
            var mestaRequest = new MestaCreateQuoteRequest
            {
                SourceCurrency = MapStablecoinToMesta(request.SourceCurrency),
                TargetCurrency = MapFiatCurrencyToMesta(request.TargetCurrency),
                SourceAmount = request.SourceAmount,
                TargetAmount = request.TargetAmount,
                Chain = MapNetworkToMesta(request.Network),
                PaymentMethod = request.PaymentMethod.HasValue ? MapPaymentMethodToMesta(request.PaymentMethod.Value) : null,
                DestinationCountry = request.DestinationCountry,
                DeveloperFee = request.DeveloperFee
            };

            var response = await PostAsync<MestaQuoteResponse>("quotes", mestaRequest, cancellationToken);

            var quote = new PayoutQuote
            {
                Id = Guid.NewGuid().ToString(),
                ProviderQuoteId = response.Id,
                SourceCurrency = request.SourceCurrency,
                TargetCurrency = request.TargetCurrency,
                SourceAmount = response.SourceAmount,
                TargetAmount = response.TargetAmount,
                ExchangeRate = response.ExchangeRate,
                FeeAmount = response.FeeAmount,
                FeeBreakdown = response.Fees != null ? new FeeBreakdown
                {
                    NetworkFee = response.Fees.NetworkFee,
                    ProcessingFee = response.Fees.ProcessingFee,
                    FxSpreadFee = response.Fees.FxSpreadFee,
                    BankFee = response.Fees.BankFee,
                    DeveloperFee = response.Fees.DeveloperFee
                } : null,
                Network = request.Network,
                CreatedAt = response.CreatedAt,
                ExpiresAt = response.ExpiresAt,
                Provider = PayoutProvider.Mesta
            };

            _logger.LogInformation(
                "Quote received from Mesta: {QuoteId}, Rate: {Rate}, Fee: {Fee}",
                response.Id,
                response.ExchangeRate,
                response.FeeAmount);

            return QuoteResult.Succeeded(quote);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get quote from Mesta");
            return QuoteResult.Failed(ex.ProviderErrorCode ?? "QUOTE_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<PayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating payout via Mesta: {SourceAmount} {SourceCurrency} -> {TargetCurrency}",
            request.SourceAmount ?? request.TargetAmount,
            request.SourceCurrency,
            request.TargetCurrency);

        try
        {
            // Create sender if no ID
            var sender = request.Sender;
            if (string.IsNullOrEmpty(sender.Id))
            {
                sender = await CreateSenderAsync(sender, cancellationToken);
            }

            // Create beneficiary if no ID
            var beneficiary = request.Beneficiary;
            if (string.IsNullOrEmpty(beneficiary.Id))
            {
                beneficiary = await CreateBeneficiaryAsync(beneficiary, cancellationToken);
            }

            // Get quote if not provided
            string quoteId = request.QuoteId ?? "";
            MestaQuoteResponse? quoteResponse = null;

            if (string.IsNullOrEmpty(quoteId))
            {
                var quoteRequest = new MestaCreateQuoteRequest
                {
                    SourceCurrency = MapStablecoinToMesta(request.SourceCurrency),
                    TargetCurrency = MapFiatCurrencyToMesta(request.TargetCurrency),
                    SourceAmount = request.SourceAmount,
                    TargetAmount = request.TargetAmount,
                    Chain = MapNetworkToMesta(request.Network),
                    PaymentMethod = MapPaymentMethodToMesta(request.PaymentMethod),
                    DestinationCountry = beneficiary.BankAccount.CountryCode,
                    DeveloperFee = request.DeveloperFee
                };

                quoteResponse = await PostAsync<MestaQuoteResponse>("quotes", quoteRequest, cancellationToken);
                quoteId = quoteResponse.Id;
            }

            // Create order
            var orderRequest = new MestaCreateOrderRequest
            {
                SenderId = sender.Id!,
                BeneficiaryId = beneficiary.Id!,
                AcceptedQuoteId = quoteId,
                ExternalId = request.ExternalId,
                Metadata = request.Metadata
            };

            var orderResponse = await PostAsync<MestaOrderResponse>("orders", orderRequest, cancellationToken);

            // Get deposit wallet
            DepositWallet? depositWallet = null;
            if (orderResponse.DepositWallet != null)
            {
                depositWallet = MapToDepositWallet(orderResponse.DepositWallet, request.SourceCurrency);
            }
            else
            {
                depositWallet = await GetDepositWalletAsync(orderResponse.Id, cancellationToken);
            }

            var payout = new Payout
            {
                Id = Guid.NewGuid().ToString(),
                ExternalId = request.ExternalId,
                Provider = PayoutProvider.Mesta,
                ProviderOrderId = orderResponse.Id,
                Status = MapMestaStatus(orderResponse.Status),
                SourceCurrency = request.SourceCurrency,
                SourceAmount = orderResponse.SourceAmount,
                TargetCurrency = request.TargetCurrency,
                TargetAmount = orderResponse.TargetAmount,
                ExchangeRate = orderResponse.ExchangeRate,
                FeeAmount = orderResponse.FeeAmount,
                Network = request.Network,
                Sender = sender,
                Beneficiary = beneficiary,
                DepositWallet = depositWallet,
                QuoteId = quoteId,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = orderResponse.CreatedAt,
                UpdatedAt = orderResponse.UpdatedAt,
                Metadata = request.Metadata
            };

            _logger.LogInformation(
                "Payout created via Mesta: {PayoutId}, OrderId: {OrderId}, Status: {Status}",
                payout.Id,
                orderResponse.Id,
                orderResponse.Status);

            return PayoutResult.Succeeded(payout);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to create payout via Mesta");
            return PayoutResult.Failed(ex.ProviderErrorCode ?? "PAYOUT_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<PayoutStatusResult> GetPayoutStatusAsync(string providerOrderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payout status from Mesta: {OrderId}", providerOrderId);

        try
        {
            var response = await GetAsync<MestaOrderResponse>($"orders/{providerOrderId}", cancellationToken);

            var statusUpdate = new PayoutStatusUpdate
            {
                PayoutId = providerOrderId,
                ProviderOrderId = response.Id,
                CurrentStatus = MapMestaStatus(response.Status),
                ProviderStatus = response.Status,
                BlockchainTxHash = response.BlockchainTxHash,
                BankReference = response.BankReference,
                FailureReason = response.FailureReason,
                Timestamp = response.UpdatedAt,
                Provider = PayoutProvider.Mesta
            };

            return PayoutStatusResult.Succeeded(statusUpdate);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get payout status from Mesta: {OrderId}", providerOrderId);
            return PayoutStatusResult.Failed(ex.ProviderErrorCode ?? "STATUS_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<DepositWallet> GetDepositWalletAsync(string providerOrderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting deposit wallet from Mesta: {OrderId}", providerOrderId);

        var response = await GetAsync<MestaWalletResponse>($"orders/{providerOrderId}/wallets", cancellationToken);
        return MapToDepositWallet(response, MapMestaStablecoin(response.Currency));
    }

    public async Task<bool> CancelPayoutAsync(string providerOrderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling payout via Mesta: {OrderId}", providerOrderId);

        try
        {
            var response = await _httpClient.PostAsync($"orders/{providerOrderId}/cancel", null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Payout cancelled via Mesta: {OrderId}", providerOrderId);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to cancel payout via Mesta: {OrderId}, Response: {Response}", providerOrderId, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payout via Mesta: {OrderId}", providerOrderId);
            return false;
        }
    }

    public bool SupportsConfiguration(Stablecoin sourceCurrency, FiatCurrency targetCurrency, BlockchainNetwork network, string destinationCountry)
    {
        return _supportedStablecoins.Contains(sourceCurrency)
            && _supportedFiatCurrencies.Contains(targetCurrency)
            && _supportedNetworks.Contains(network)
            && _supportedCountries.Contains(destinationCountry);
    }

    #region Webhook Handling

    public bool ValidateSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_settings.WebhookSecret))
        {
            _logger.LogWarning("Webhook secret not configured for Mesta");
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

        return string.Equals(signature, computedSignature, StringComparison.OrdinalIgnoreCase);
    }

    public Task<PayoutStatusUpdate?> ProcessWebhookAsync(string payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookPayload = JsonSerializer.Deserialize<MestaWebhookPayload>(payload, _jsonOptions);

            if (webhookPayload?.Data == null)
            {
                _logger.LogWarning("Invalid Mesta webhook payload");
                return Task.FromResult<PayoutStatusUpdate?>(null);
            }

            var statusUpdate = new PayoutStatusUpdate
            {
                PayoutId = webhookPayload.Data.ExternalId ?? webhookPayload.Data.Id,
                ProviderOrderId = webhookPayload.Data.Id,
                CurrentStatus = MapMestaStatus(webhookPayload.Data.Status),
                ProviderStatus = webhookPayload.Data.Status,
                BlockchainTxHash = webhookPayload.Data.BlockchainTxHash,
                BankReference = webhookPayload.Data.BankReference,
                FailureReason = webhookPayload.Data.FailureReason,
                Timestamp = webhookPayload.Timestamp,
                Provider = PayoutProvider.Mesta
            };

            _logger.LogInformation(
                "Processed Mesta webhook: Event={Event}, OrderId={OrderId}, Status={Status}",
                webhookPayload.Event,
                webhookPayload.Data.Id,
                webhookPayload.Data.Status);

            return Task.FromResult<PayoutStatusUpdate?>(statusUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Mesta webhook");
            return Task.FromResult<PayoutStatusUpdate?>(null);
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        return await HandleResponseAsync<T>(response, cancellationToken);
    }

    private async Task<T> PostAsync<T>(string endpoint, object request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, request, _jsonOptions, cancellationToken);
        return await HandleResponseAsync<T>(response, cancellationToken);
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            MestaError? error = null;
            try
            {
                var errorResponse = JsonSerializer.Deserialize<MestaResponse<object>>(content, _jsonOptions);
                error = errorResponse?.Error;
            }
            catch
            {
                // Ignore deserialization errors
            }

            throw new ProviderApiException(
                PayoutProvider.Mesta,
                $"Mesta API error: {response.StatusCode}",
                (int)response.StatusCode,
                error?.Code,
                error?.Message ?? content);
        }

        var result = JsonSerializer.Deserialize<MestaResponse<T>>(content, _jsonOptions);

        if (result?.Data == null)
        {
            throw new ProviderApiException(
                PayoutProvider.Mesta,
                "Invalid response from Mesta API",
                (int)response.StatusCode);
        }

        return result.Data;
    }

    private static MestaCreateSenderRequest MapToMestaSender(Sender sender)
    {
        return new MestaCreateSenderRequest
        {
            Type = sender.Type == BeneficiaryType.Individual ? "individual" : "business",
            FirstName = sender.FirstName,
            LastName = sender.LastName,
            BusinessName = sender.BusinessName,
            Email = sender.Email,
            Phone = sender.PhoneNumber,
            DateOfBirth = sender.DateOfBirth?.ToString("yyyy-MM-dd"),
            Identity = !string.IsNullOrEmpty(sender.DocumentNumber) ? new MestaIdentity
            {
                DocumentType = sender.DocumentType,
                DocumentNumber = sender.DocumentNumber,
                Nationality = sender.Nationality
            } : null,
            Address = sender.Address != null ? new MestaAddress
            {
                Street1 = sender.Address.Street1,
                Street2 = sender.Address.Street2,
                City = sender.Address.City,
                State = sender.Address.State,
                PostalCode = sender.Address.PostalCode,
                Country = sender.Address.CountryCode
            } : null,
            ExternalId = sender.ExternalId
        };
    }

    private static MestaCreateBeneficiaryRequest MapToMestaBeneficiary(Beneficiary beneficiary)
    {
        return new MestaCreateBeneficiaryRequest
        {
            Type = beneficiary.Type == BeneficiaryType.Individual ? "individual" : "business",
            FirstName = beneficiary.FirstName,
            LastName = beneficiary.LastName,
            BusinessName = beneficiary.BusinessName,
            Email = beneficiary.Email,
            Phone = beneficiary.PhoneNumber,
            DateOfBirth = beneficiary.DateOfBirth?.ToString("yyyy-MM-dd"),
            Identity = !string.IsNullOrEmpty(beneficiary.DocumentNumber) ? new MestaIdentity
            {
                DocumentType = beneficiary.DocumentType,
                DocumentNumber = beneficiary.DocumentNumber,
                Nationality = beneficiary.Nationality
            } : null,
            Address = beneficiary.Address != null ? new MestaAddress
            {
                Street1 = beneficiary.Address.Street1,
                Street2 = beneficiary.Address.Street2,
                City = beneficiary.Address.City,
                State = beneficiary.Address.State,
                PostalCode = beneficiary.Address.PostalCode,
                Country = beneficiary.Address.CountryCode
            } : null,
            PaymentInfo = new MestaPaymentInfo
            {
                BankName = beneficiary.BankAccount.BankName,
                AccountNumber = beneficiary.BankAccount.AccountNumber,
                AccountHolderName = beneficiary.BankAccount.AccountHolderName,
                RoutingNumber = beneficiary.BankAccount.RoutingNumber,
                SwiftCode = beneficiary.BankAccount.SwiftCode,
                SortCode = beneficiary.BankAccount.SortCode,
                Iban = beneficiary.BankAccount.Iban,
                Currency = MapFiatCurrencyToMesta(beneficiary.BankAccount.Currency),
                Country = beneficiary.BankAccount.CountryCode,
                BranchCode = beneficiary.BankAccount.BranchCode
            },
            ExternalId = beneficiary.ExternalId
        };
    }

    private static DepositWallet MapToDepositWallet(MestaWalletResponse wallet, Stablecoin currency)
    {
        return new DepositWallet
        {
            Id = wallet.Id,
            Address = wallet.Address,
            Network = MapMestaNetwork(wallet.Chain),
            Currency = currency,
            ExpectedAmount = wallet.ExpectedAmount,
            ExpiresAt = wallet.ExpiresAt,
            Memo = wallet.Memo
        };
    }

    private static string MapStablecoinToMesta(Stablecoin coin) => coin switch
    {
        Stablecoin.USDC => "USDC",
        Stablecoin.USDT => "USDT",
        _ => throw new ArgumentOutOfRangeException(nameof(coin))
    };

    private static Stablecoin MapMestaStablecoin(string currency) => currency.ToUpperInvariant() switch
    {
        "USDC" => Stablecoin.USDC,
        "USDT" => Stablecoin.USDT,
        _ => throw new ArgumentOutOfRangeException(nameof(currency))
    };

    private static string MapFiatCurrencyToMesta(FiatCurrency currency) => currency switch
    {
        FiatCurrency.USD => "USD",
        FiatCurrency.EUR => "EUR",
        FiatCurrency.GBP => "GBP",
        _ => throw new ArgumentOutOfRangeException(nameof(currency))
    };

    private static string MapNetworkToMesta(BlockchainNetwork network) => network switch
    {
        BlockchainNetwork.Ethereum => "ethereum",
        BlockchainNetwork.Polygon => "polygon",
        BlockchainNetwork.Arbitrum => "arbitrum",
        BlockchainNetwork.Optimism => "optimism",
        BlockchainNetwork.Base => "base",
        BlockchainNetwork.Tron => "tron",
        BlockchainNetwork.Solana => "solana",
        _ => throw new ArgumentOutOfRangeException(nameof(network))
    };

    private static BlockchainNetwork MapMestaNetwork(string chain) => chain.ToLowerInvariant() switch
    {
        "ethereum" or "eth" => BlockchainNetwork.Ethereum,
        "polygon" or "matic" or "pol" => BlockchainNetwork.Polygon,
        "arbitrum" or "arb" => BlockchainNetwork.Arbitrum,
        "optimism" or "op" => BlockchainNetwork.Optimism,
        "base" => BlockchainNetwork.Base,
        "tron" or "trx" => BlockchainNetwork.Tron,
        "solana" or "sol" => BlockchainNetwork.Solana,
        _ => throw new ArgumentOutOfRangeException(nameof(chain))
    };

    private static string MapPaymentMethodToMesta(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "bank_transfer",
        PaymentMethod.Sepa => "sepa",
        PaymentMethod.Ach => "ach",
        PaymentMethod.FasterPayments => "faster_payments",
        PaymentMethod.Swift => "swift",
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static PayoutStatus MapMestaStatus(string status) => status.ToLowerInvariant() switch
    {
        "created" => PayoutStatus.Created,
        "awaiting_funds" => PayoutStatus.AwaitingFunds,
        "awaiting_funds_timeout" => PayoutStatus.Expired,
        "funds_received" => PayoutStatus.FundsReceived,
        "in_progress" or "processing" => PayoutStatus.Processing,
        "sent_to_beneficiary" => PayoutStatus.SentToBeneficiary,
        "completed" or "success" => PayoutStatus.Completed,
        "failed" or "error" => PayoutStatus.Failed,
        "cancelled" or "canceled" => PayoutStatus.Cancelled,
        "need_review" or "pending_review" => PayoutStatus.PendingReview,
        "refunded" => PayoutStatus.Refunded,
        _ => PayoutStatus.Processing
    };

    #endregion
}
