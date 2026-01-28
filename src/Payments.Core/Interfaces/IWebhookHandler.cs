using Payments.Core.Enums;
using Payments.Core.Models;

namespace Payments.Core.Interfaces;

/// <summary>
/// Interface for handling webhooks from payout providers.
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Gets the provider this handler is for.
    /// </summary>
    PayoutProvider Provider { get; }

    /// <summary>
    /// Validates the webhook signature.
    /// </summary>
    /// <param name="payload">Raw webhook payload.</param>
    /// <param name="signature">Signature from the webhook request headers.</param>
    /// <returns>True if the signature is valid.</returns>
    bool ValidateSignature(string payload, string signature);

    /// <summary>
    /// Processes a webhook payload and returns the status update.
    /// </summary>
    /// <param name="payload">Raw webhook payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted status update.</returns>
    Task<PayoutStatusUpdate?> ProcessWebhookAsync(string payload, CancellationToken cancellationToken = default);
}
