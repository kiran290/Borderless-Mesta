using Payments.Core.Enums;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;

namespace Payments.Core.Interfaces;

/// <summary>
/// High-level service interface for payout operations.
/// Orchestrates provider selection and payout execution.
/// </summary>
public interface IPayoutService
{
    /// <summary>
    /// Gets a quote for a stablecoin to fiat payout.
    /// </summary>
    /// <param name="request">Quote request parameters.</param>
    /// <param name="provider">Optional specific provider to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Quote result containing exchange rate and fee information.</returns>
    Task<QuoteResult> GetQuoteAsync(CreateQuoteRequest request, PayoutProvider? provider = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets quotes from all available providers for comparison.
    /// </summary>
    /// <param name="request">Quote request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of quotes from different providers.</returns>
    Task<IReadOnlyList<QuoteResult>> GetAllQuotesAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a stablecoin to fiat payout.
    /// </summary>
    /// <param name="request">Payout request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payout result containing the created payout and deposit wallet information.</returns>
    Task<PayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a payout.
    /// </summary>
    /// <param name="payoutId">The internal payout ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current payout status.</returns>
    Task<PayoutStatusResult> GetPayoutStatusAsync(string payoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the deposit wallet address for a payout.
    /// </summary>
    /// <param name="payoutId">The internal payout ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deposit wallet information.</returns>
    Task<DepositWallet> GetDepositWalletAsync(string payoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a payout if possible.
    /// </summary>
    /// <param name="payoutId">The internal payout ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cancellation was successful.</returns>
    Task<bool> CancelPayoutAsync(string payoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payout history for a specific sender or beneficiary.
    /// </summary>
    /// <param name="senderId">Optional sender ID filter.</param>
    /// <param name="beneficiaryId">Optional beneficiary ID filter.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of payouts matching the criteria.</returns>
    Task<IReadOnlyList<Payout>> GetPayoutHistoryAsync(
        string? senderId = null,
        string? beneficiaryId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
}
