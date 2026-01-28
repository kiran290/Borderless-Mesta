using Payments.Core.Enums;
using Payments.Core.Models;

namespace Payments.Api.Dtos;

/// <summary>
/// Standard API response wrapper.
/// </summary>
/// <typeparam name="T">Data type.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Response data.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Error information (if not successful).
    /// </summary>
    public ApiError? Error { get; init; }

    /// <summary>
    /// Request ID for tracking.
    /// </summary>
    public string? RequestId { get; init; }

    public static ApiResponse<T> Ok(T data, string? requestId = null) => new()
    {
        Success = true,
        Data = data,
        RequestId = requestId
    };

    public static ApiResponse<T> Fail(string code, string message, string? requestId = null) => new()
    {
        Success = false,
        Error = new ApiError { Code = code, Message = message },
        RequestId = requestId
    };
}

/// <summary>
/// API error information.
/// </summary>
public sealed class ApiError
{
    /// <summary>
    /// Error code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Field-level validation errors.
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
}

/// <summary>
/// Quote response DTO.
/// </summary>
public sealed class QuoteResponseDto
{
    public required string Id { get; init; }
    public required Stablecoin SourceCurrency { get; init; }
    public required FiatCurrency TargetCurrency { get; init; }
    public required decimal SourceAmount { get; init; }
    public required decimal TargetAmount { get; init; }
    public required decimal ExchangeRate { get; init; }
    public required decimal FeeAmount { get; init; }
    public FeeBreakdownDto? FeeBreakdown { get; init; }
    public required BlockchainNetwork Network { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required PayoutProvider Provider { get; init; }

    public static QuoteResponseDto FromModel(PayoutQuote quote) => new()
    {
        Id = quote.ProviderQuoteId ?? quote.Id,
        SourceCurrency = quote.SourceCurrency,
        TargetCurrency = quote.TargetCurrency,
        SourceAmount = quote.SourceAmount,
        TargetAmount = quote.TargetAmount,
        ExchangeRate = quote.ExchangeRate,
        FeeAmount = quote.FeeAmount,
        FeeBreakdown = quote.FeeBreakdown != null ? FeeBreakdownDto.FromModel(quote.FeeBreakdown) : null,
        Network = quote.Network,
        CreatedAt = quote.CreatedAt,
        ExpiresAt = quote.ExpiresAt,
        Provider = quote.Provider
    };
}

/// <summary>
/// Fee breakdown response DTO.
/// </summary>
public sealed class FeeBreakdownDto
{
    public decimal NetworkFee { get; init; }
    public decimal ProcessingFee { get; init; }
    public decimal FxSpreadFee { get; init; }
    public decimal BankFee { get; init; }
    public decimal DeveloperFee { get; init; }
    public decimal Total { get; init; }

    public static FeeBreakdownDto FromModel(FeeBreakdown fees) => new()
    {
        NetworkFee = fees.NetworkFee,
        ProcessingFee = fees.ProcessingFee,
        FxSpreadFee = fees.FxSpreadFee,
        BankFee = fees.BankFee,
        DeveloperFee = fees.DeveloperFee,
        Total = fees.Total
    };
}

/// <summary>
/// Payout response DTO.
/// </summary>
public sealed class PayoutResponseDto
{
    public required string Id { get; init; }
    public string? ExternalId { get; init; }
    public required PayoutProvider Provider { get; init; }
    public required string ProviderOrderId { get; init; }
    public required PayoutStatus Status { get; init; }
    public required Stablecoin SourceCurrency { get; init; }
    public required decimal SourceAmount { get; init; }
    public required FiatCurrency TargetCurrency { get; init; }
    public required decimal TargetAmount { get; init; }
    public required decimal ExchangeRate { get; init; }
    public required decimal FeeAmount { get; init; }
    public required BlockchainNetwork Network { get; init; }
    public DepositWalletDto? DepositWallet { get; init; }
    public string? QuoteId { get; init; }
    public required PaymentMethod PaymentMethod { get; init; }
    public string? BlockchainTxHash { get; init; }
    public string? BankReference { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }

    public static PayoutResponseDto FromModel(Payout payout) => new()
    {
        Id = payout.Id,
        ExternalId = payout.ExternalId,
        Provider = payout.Provider,
        ProviderOrderId = payout.ProviderOrderId,
        Status = payout.Status,
        SourceCurrency = payout.SourceCurrency,
        SourceAmount = payout.SourceAmount,
        TargetCurrency = payout.TargetCurrency,
        TargetAmount = payout.TargetAmount,
        ExchangeRate = payout.ExchangeRate,
        FeeAmount = payout.FeeAmount,
        Network = payout.Network,
        DepositWallet = payout.DepositWallet != null ? DepositWalletDto.FromModel(payout.DepositWallet) : null,
        QuoteId = payout.QuoteId,
        PaymentMethod = payout.PaymentMethod,
        BlockchainTxHash = payout.BlockchainTxHash,
        BankReference = payout.BankReference,
        FailureReason = payout.FailureReason,
        CreatedAt = payout.CreatedAt,
        UpdatedAt = payout.UpdatedAt,
        CompletedAt = payout.CompletedAt
    };
}

/// <summary>
/// Deposit wallet response DTO.
/// </summary>
public sealed class DepositWalletDto
{
    public required string Id { get; init; }
    public required string Address { get; init; }
    public required BlockchainNetwork Network { get; init; }
    public required Stablecoin Currency { get; init; }
    public required decimal ExpectedAmount { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? Memo { get; init; }

    public static DepositWalletDto FromModel(DepositWallet wallet) => new()
    {
        Id = wallet.Id,
        Address = wallet.Address,
        Network = wallet.Network,
        Currency = wallet.Currency,
        ExpectedAmount = wallet.ExpectedAmount,
        ExpiresAt = wallet.ExpiresAt,
        Memo = wallet.Memo
    };
}

/// <summary>
/// Payout status response DTO.
/// </summary>
public sealed class PayoutStatusResponseDto
{
    public required string PayoutId { get; init; }
    public required string ProviderOrderId { get; init; }
    public required PayoutStatus Status { get; init; }
    public string? ProviderStatus { get; init; }
    public string? BlockchainTxHash { get; init; }
    public string? BankReference { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required PayoutProvider Provider { get; init; }

    public static PayoutStatusResponseDto FromModel(PayoutStatusUpdate update) => new()
    {
        PayoutId = update.PayoutId,
        ProviderOrderId = update.ProviderOrderId,
        Status = update.CurrentStatus,
        ProviderStatus = update.ProviderStatus,
        BlockchainTxHash = update.BlockchainTxHash,
        BankReference = update.BankReference,
        FailureReason = update.FailureReason,
        Timestamp = update.Timestamp,
        Provider = update.Provider
    };
}

/// <summary>
/// Provider information DTO.
/// </summary>
public sealed class ProviderInfoDto
{
    public required PayoutProvider Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<Stablecoin> SupportedStablecoins { get; init; }
    public required IReadOnlyList<FiatCurrency> SupportedFiatCurrencies { get; init; }
    public required IReadOnlyList<BlockchainNetwork> SupportedNetworks { get; init; }
    public required bool IsAvailable { get; init; }
}
