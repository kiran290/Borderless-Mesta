using System.Net.Http.Headers;
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

namespace Payments.Infrastructure.Providers.Borderless;

/// <summary>
/// Borderless payout provider implementation.
/// Implements stablecoin to fiat payouts via Borderless API.
/// </summary>
public sealed class BorderlessPayoutProvider : IPayoutProvider, IWebhookHandler
{
    private readonly HttpClient _httpClient;
    private readonly BorderlessSettings _settings;
    private readonly ILogger<BorderlessPayoutProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

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
        BlockchainNetwork.Optimism,
        BlockchainNetwork.Base,
        BlockchainNetwork.Tron,
        BlockchainNetwork.Solana
    };

    private static readonly HashSet<string> _supportedCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "GB", "DE", "FR", "ES", "IT", "NL", "BE", "AT", "PT",
        "IE", "FI", "SE", "DK", "NO", "CH", "PL", "CZ", "HU", "RO",
        "BG", "HR", "SK", "SI", "LT", "LV", "EE", "MT", "CY", "LU",
        "GR", "MX", "BR", "AR", "CL", "CO", "PE", "IN", "PH", "ID",
        "MY", "SG", "TH", "VN", "AU", "NZ", "JP", "KR", "HK", "TW",
        "ZA", "NG", "KE", "GH", "AE", "SA", "IL", "TR", "EG", "MA"
    };

    public PayoutProvider ProviderId => PayoutProvider.Borderless;
    public string ProviderName => "Borderless";
    public IReadOnlyList<Stablecoin> SupportedStablecoins => _supportedStablecoins;
    public IReadOnlyList<FiatCurrency> SupportedFiatCurrencies => _supportedFiatCurrencies;
    public IReadOnlyList<BlockchainNetwork> SupportedNetworks => _supportedNetworks;
    PayoutProvider IWebhookHandler.Provider => PayoutProvider.Borderless;

    public BorderlessPayoutProvider(
        HttpClient httpClient,
        IOptions<BorderlessSettings> settings,
        ILogger<BorderlessPayoutProvider> logger)
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
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            return;
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                return;
            }

            _logger.LogInformation("Authenticating with Borderless API");

            var authRequest = new BorderlessAuthRequest
            {
                ClientId = _settings.ClientId,
                ApiKey = _settings.ApiKey,
                ApiSecret = _settings.ApiSecret
            };

            var response = await _httpClient.PostAsJsonAsync("auth/token", authRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ProviderAuthenticationException(PayoutProvider.Borderless, $"Authentication failed: {content}");
            }

            var authResponse = JsonSerializer.Deserialize<BorderlessResponse<BorderlessAuthResponse>>(content, _jsonOptions);

            if (authResponse?.Data == null)
            {
                throw new ProviderAuthenticationException(PayoutProvider.Borderless, "Invalid authentication response");
            }

            _accessToken = authResponse.Data.AccessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(authResponse.Data.ExpiresIn - 60); // Refresh 1 minute early

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            _logger.LogInformation("Successfully authenticated with Borderless API");
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            var response = await _httpClient.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Borderless provider availability check failed");
            return false;
        }
    }

    public async Task<Sender> CreateSenderAsync(Sender sender, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating sender in Borderless: {Email}", sender.Email);

        await EnsureAuthenticatedAsync(cancellationToken);

        var request = MapToBorderlessCustomer(sender, "sender");
        var response = await PostAsync<BorderlessCustomerResponse>("customers", request, cancellationToken);

        sender.Id = response.Id;
        _logger.LogInformation("Sender created in Borderless with ID: {SenderId}", response.Id);

        return sender;
    }

    public async Task<Beneficiary> CreateBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating beneficiary in Borderless: {Email}", beneficiary.Email);

        await EnsureAuthenticatedAsync(cancellationToken);

        var request = MapToBorderlessCustomer(beneficiary);
        var response = await PostAsync<BorderlessCustomerResponse>("customers", request, cancellationToken);

        beneficiary.Id = response.Id;
        _logger.LogInformation("Beneficiary created in Borderless with ID: {BeneficiaryId}", response.Id);

        return beneficiary;
    }

    public async Task<QuoteResult> GetQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting quote from Borderless: {SourceCurrency} -> {TargetCurrency}",
            request.SourceCurrency,
            request.TargetCurrency);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new BorderlessCreateQuoteRequest
            {
                SourceAsset = MapStablecoinToBorderless(request.SourceCurrency),
                SourceNetwork = MapNetworkToBorderless(request.Network),
                TargetCurrency = MapFiatCurrencyToBorderless(request.TargetCurrency),
                TargetCountry = request.DestinationCountry,
                SourceAmount = request.SourceAmount,
                TargetAmount = request.TargetAmount,
                PaymentRail = request.PaymentMethod.HasValue ? MapPaymentMethodToBorderless(request.PaymentMethod.Value) : null
            };

            var response = await PostAsync<BorderlessQuoteResponse>("quotes", borderlessRequest, cancellationToken);

            var quote = new PayoutQuote
            {
                Id = Guid.NewGuid().ToString(),
                ProviderQuoteId = response.QuoteId,
                SourceCurrency = request.SourceCurrency,
                TargetCurrency = request.TargetCurrency,
                SourceAmount = response.SourceAmount,
                TargetAmount = response.TargetAmount,
                ExchangeRate = response.ExchangeRate,
                FeeAmount = response.TotalFee,
                FeeBreakdown = new FeeBreakdown
                {
                    NetworkFee = response.Fees.NetworkFee,
                    ProcessingFee = response.Fees.ProcessingFee,
                    FxSpreadFee = response.Fees.FxFee,
                    BankFee = response.Fees.SettlementFee,
                    DeveloperFee = response.Fees.PartnerFee
                },
                Network = request.Network,
                CreatedAt = response.CreatedAt,
                ExpiresAt = response.ExpiresAt,
                Provider = PayoutProvider.Borderless
            };

            _logger.LogInformation(
                "Quote received from Borderless: {QuoteId}, Rate: {Rate}, Fee: {Fee}",
                response.QuoteId,
                response.ExchangeRate,
                response.TotalFee);

            return QuoteResult.Succeeded(quote);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get quote from Borderless");
            return QuoteResult.Failed(ex.ProviderErrorCode ?? "QUOTE_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<PayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating payout via Borderless: {SourceAmount} {SourceCurrency} -> {TargetCurrency}",
            request.SourceAmount ?? request.TargetAmount,
            request.SourceCurrency,
            request.TargetCurrency);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

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

            if (string.IsNullOrEmpty(quoteId))
            {
                var quoteRequest = new BorderlessCreateQuoteRequest
                {
                    SourceAsset = MapStablecoinToBorderless(request.SourceCurrency),
                    SourceNetwork = MapNetworkToBorderless(request.Network),
                    TargetCurrency = MapFiatCurrencyToBorderless(request.TargetCurrency),
                    TargetCountry = beneficiary.BankAccount.CountryCode,
                    SourceAmount = request.SourceAmount,
                    TargetAmount = request.TargetAmount,
                    PaymentRail = MapPaymentMethodToBorderless(request.PaymentMethod)
                };

                var quoteResponse = await PostAsync<BorderlessQuoteResponse>("quotes", quoteRequest, cancellationToken);
                quoteId = quoteResponse.QuoteId;
            }

            // Create offramp transaction
            var offrampRequest = new BorderlessCreateOfframpRequest
            {
                QuoteId = quoteId,
                SenderId = sender.Id!,
                BeneficiaryId = beneficiary.Id!,
                Purpose = "payout",
                Reference = request.ExternalId,
                ExternalId = request.ExternalId,
                Metadata = request.Metadata
            };

            var offrampResponse = await PostAsync<BorderlessOfframpResponse>("offramps", offrampRequest, cancellationToken);

            // Map deposit address
            DepositWallet? depositWallet = null;
            if (offrampResponse.DepositAddress != null)
            {
                depositWallet = MapToDepositWallet(offrampResponse.DepositAddress, request.SourceCurrency);
            }

            var payout = new Payout
            {
                Id = Guid.NewGuid().ToString(),
                ExternalId = request.ExternalId,
                Provider = PayoutProvider.Borderless,
                ProviderOrderId = offrampResponse.TransactionId,
                Status = MapBorderlessStatus(offrampResponse.Status),
                SourceCurrency = request.SourceCurrency,
                SourceAmount = offrampResponse.SourceAmount,
                TargetCurrency = request.TargetCurrency,
                TargetAmount = offrampResponse.TargetAmount,
                ExchangeRate = offrampResponse.ExchangeRate,
                FeeAmount = offrampResponse.TotalFee,
                Network = request.Network,
                Sender = sender,
                Beneficiary = beneficiary,
                DepositWallet = depositWallet,
                QuoteId = quoteId,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = offrampResponse.CreatedAt,
                UpdatedAt = offrampResponse.UpdatedAt,
                Metadata = request.Metadata
            };

            _logger.LogInformation(
                "Payout created via Borderless: {PayoutId}, TransactionId: {TransactionId}, Status: {Status}",
                payout.Id,
                offrampResponse.TransactionId,
                offrampResponse.Status);

            return PayoutResult.Succeeded(payout);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to create payout via Borderless");
            return PayoutResult.Failed(ex.ProviderErrorCode ?? "PAYOUT_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<PayoutStatusResult> GetPayoutStatusAsync(string providerOrderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payout status from Borderless: {TransactionId}", providerOrderId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await GetAsync<BorderlessOfframpResponse>($"offramps/{providerOrderId}", cancellationToken);

            var statusUpdate = new PayoutStatusUpdate
            {
                PayoutId = response.ExternalId ?? providerOrderId,
                ProviderOrderId = response.TransactionId,
                CurrentStatus = MapBorderlessStatus(response.Status),
                ProviderStatus = response.Status,
                BlockchainTxHash = response.BlockchainTxHash,
                BankReference = response.BankReference,
                FailureReason = response.FailureReason,
                Timestamp = response.UpdatedAt,
                Provider = PayoutProvider.Borderless
            };

            return PayoutStatusResult.Succeeded(statusUpdate);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get payout status from Borderless: {TransactionId}", providerOrderId);
            return PayoutStatusResult.Failed(ex.ProviderErrorCode ?? "STATUS_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<DepositWallet> GetDepositWalletAsync(string providerOrderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting deposit wallet from Borderless: {TransactionId}", providerOrderId);

        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await GetAsync<BorderlessOfframpResponse>($"offramps/{providerOrderId}", cancellationToken);

        if (response.DepositAddress == null)
        {
            throw new PayoutException("DEPOSIT_ADDRESS_NOT_READY", "Deposit address not yet available", PayoutProvider.Borderless);
        }

        return MapToDepositWallet(response.DepositAddress, MapBorderlessStablecoin(response.SourceAsset));
    }

    public async Task<bool> CancelPayoutAsync(string providerOrderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling payout via Borderless: {TransactionId}", providerOrderId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.PostAsync($"offramps/{providerOrderId}/cancel", null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Payout cancelled via Borderless: {TransactionId}", providerOrderId);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to cancel payout via Borderless: {TransactionId}, Response: {Response}", providerOrderId, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payout via Borderless: {TransactionId}", providerOrderId);
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
            _logger.LogWarning("Webhook secret not configured for Borderless");
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
            var webhookPayload = JsonSerializer.Deserialize<BorderlessWebhookPayload>(payload, _jsonOptions);

            if (webhookPayload?.Data == null)
            {
                _logger.LogWarning("Invalid Borderless webhook payload");
                return Task.FromResult<PayoutStatusUpdate?>(null);
            }

            var statusUpdate = new PayoutStatusUpdate
            {
                PayoutId = webhookPayload.Data.ExternalId ?? webhookPayload.Data.TransactionId,
                ProviderOrderId = webhookPayload.Data.TransactionId,
                CurrentStatus = MapBorderlessStatus(webhookPayload.Data.Status),
                ProviderStatus = webhookPayload.Data.Status,
                BlockchainTxHash = webhookPayload.Data.BlockchainTxHash,
                BankReference = webhookPayload.Data.BankReference,
                FailureReason = webhookPayload.Data.FailureReason,
                Timestamp = webhookPayload.Timestamp,
                Provider = PayoutProvider.Borderless
            };

            _logger.LogInformation(
                "Processed Borderless webhook: Event={Event}, TransactionId={TransactionId}, Status={Status}",
                webhookPayload.EventType,
                webhookPayload.Data.TransactionId,
                webhookPayload.Data.Status);

            return Task.FromResult<PayoutStatusUpdate?>(statusUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Borderless webhook");
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
            BorderlessError? error = null;
            try
            {
                var errorResponse = JsonSerializer.Deserialize<BorderlessResponse<object>>(content, _jsonOptions);
                error = errorResponse?.Error;
            }
            catch
            {
                // Ignore deserialization errors
            }

            throw new ProviderApiException(
                PayoutProvider.Borderless,
                $"Borderless API error: {response.StatusCode}",
                (int)response.StatusCode,
                error?.Code,
                error?.Message ?? content);
        }

        var result = JsonSerializer.Deserialize<BorderlessResponse<T>>(content, _jsonOptions);

        if (result?.Data == null)
        {
            throw new ProviderApiException(
                PayoutProvider.Borderless,
                "Invalid response from Borderless API",
                (int)response.StatusCode);
        }

        return result.Data;
    }

    private static BorderlessCreateCustomerRequest MapToBorderlessCustomer(Sender sender, string role)
    {
        return new BorderlessCreateCustomerRequest
        {
            Type = sender.Type == BeneficiaryType.Individual ? "individual" : "business",
            Role = role,
            FirstName = sender.FirstName,
            LastName = sender.LastName,
            CompanyName = sender.BusinessName,
            Email = sender.Email,
            Phone = sender.PhoneNumber,
            DateOfBirth = sender.DateOfBirth?.ToString("yyyy-MM-dd"),
            Nationality = sender.Nationality,
            Address = sender.Address != null ? new BorderlessAddress
            {
                Line1 = sender.Address.Street1,
                Line2 = sender.Address.Street2,
                City = sender.Address.City,
                State = sender.Address.State,
                PostalCode = sender.Address.PostalCode,
                Country = sender.Address.CountryCode
            } : null,
            Identification = !string.IsNullOrEmpty(sender.DocumentNumber) ? new BorderlessIdentification
            {
                Type = sender.DocumentType ?? "passport",
                Number = sender.DocumentNumber
            } : null,
            ExternalId = sender.ExternalId
        };
    }

    private static BorderlessCreateCustomerRequest MapToBorderlessCustomer(Beneficiary beneficiary)
    {
        return new BorderlessCreateCustomerRequest
        {
            Type = beneficiary.Type == BeneficiaryType.Individual ? "individual" : "business",
            Role = "beneficiary",
            FirstName = beneficiary.FirstName,
            LastName = beneficiary.LastName,
            CompanyName = beneficiary.BusinessName,
            Email = beneficiary.Email,
            Phone = beneficiary.PhoneNumber,
            DateOfBirth = beneficiary.DateOfBirth?.ToString("yyyy-MM-dd"),
            Nationality = beneficiary.Nationality,
            Address = beneficiary.Address != null ? new BorderlessAddress
            {
                Line1 = beneficiary.Address.Street1,
                Line2 = beneficiary.Address.Street2,
                City = beneficiary.Address.City,
                State = beneficiary.Address.State,
                PostalCode = beneficiary.Address.PostalCode,
                Country = beneficiary.Address.CountryCode
            } : null,
            Identification = !string.IsNullOrEmpty(beneficiary.DocumentNumber) ? new BorderlessIdentification
            {
                Type = beneficiary.DocumentType ?? "passport",
                Number = beneficiary.DocumentNumber
            } : null,
            BankAccount = new BorderlessBankAccount
            {
                BankName = beneficiary.BankAccount.BankName,
                AccountNumber = beneficiary.BankAccount.AccountNumber,
                AccountName = beneficiary.BankAccount.AccountHolderName,
                RoutingNumber = beneficiary.BankAccount.RoutingNumber,
                SwiftCode = beneficiary.BankAccount.SwiftCode,
                SortCode = beneficiary.BankAccount.SortCode,
                Iban = beneficiary.BankAccount.Iban,
                Currency = MapFiatCurrencyToBorderless(beneficiary.BankAccount.Currency),
                Country = beneficiary.BankAccount.CountryCode,
                BranchCode = beneficiary.BankAccount.BranchCode
            },
            ExternalId = beneficiary.ExternalId
        };
    }

    private static DepositWallet MapToDepositWallet(BorderlessDepositAddress address, Stablecoin currency)
    {
        return new DepositWallet
        {
            Id = address.Id,
            Address = address.Address,
            Network = MapBorderlessNetwork(address.Network),
            Currency = currency,
            ExpectedAmount = address.ExpectedAmount,
            ExpiresAt = address.ExpiresAt,
            Memo = address.Memo ?? address.Tag
        };
    }

    private static string MapStablecoinToBorderless(Stablecoin coin) => coin switch
    {
        Stablecoin.USDC => "USDC",
        Stablecoin.USDT => "USDT",
        _ => throw new ArgumentOutOfRangeException(nameof(coin))
    };

    private static Stablecoin MapBorderlessStablecoin(string asset) => asset.ToUpperInvariant() switch
    {
        "USDC" => Stablecoin.USDC,
        "USDT" => Stablecoin.USDT,
        _ => throw new ArgumentOutOfRangeException(nameof(asset))
    };

    private static string MapFiatCurrencyToBorderless(FiatCurrency currency) => currency switch
    {
        FiatCurrency.USD => "USD",
        FiatCurrency.EUR => "EUR",
        FiatCurrency.GBP => "GBP",
        _ => throw new ArgumentOutOfRangeException(nameof(currency))
    };

    private static string MapNetworkToBorderless(BlockchainNetwork network) => network switch
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

    private static BlockchainNetwork MapBorderlessNetwork(string network) => network.ToLowerInvariant() switch
    {
        "ethereum" or "eth" => BlockchainNetwork.Ethereum,
        "polygon" or "matic" => BlockchainNetwork.Polygon,
        "arbitrum" or "arb" => BlockchainNetwork.Arbitrum,
        "optimism" or "op" => BlockchainNetwork.Optimism,
        "base" => BlockchainNetwork.Base,
        "tron" or "trx" => BlockchainNetwork.Tron,
        "solana" or "sol" => BlockchainNetwork.Solana,
        _ => throw new ArgumentOutOfRangeException(nameof(network))
    };

    private static string MapPaymentMethodToBorderless(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "local_bank",
        PaymentMethod.Sepa => "sepa",
        PaymentMethod.Ach => "ach",
        PaymentMethod.FasterPayments => "faster_payments",
        PaymentMethod.Swift => "swift",
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static PayoutStatus MapBorderlessStatus(string status) => status.ToLowerInvariant() switch
    {
        "pending" or "created" => PayoutStatus.Created,
        "awaiting_deposit" or "awaiting_funds" => PayoutStatus.AwaitingFunds,
        "deposit_received" or "funds_received" => PayoutStatus.FundsReceived,
        "processing" or "in_progress" => PayoutStatus.Processing,
        "settlement_initiated" or "sent_to_beneficiary" => PayoutStatus.SentToBeneficiary,
        "completed" or "settled" or "success" => PayoutStatus.Completed,
        "failed" or "error" => PayoutStatus.Failed,
        "cancelled" or "canceled" => PayoutStatus.Cancelled,
        "expired" => PayoutStatus.Expired,
        "under_review" or "pending_review" => PayoutStatus.PendingReview,
        "refunded" => PayoutStatus.Refunded,
        _ => PayoutStatus.Processing
    };

    #endregion
}
