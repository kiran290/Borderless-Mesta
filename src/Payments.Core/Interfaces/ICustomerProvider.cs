using Payments.Core.Enums;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;

namespace Payments.Core.Interfaces;

/// <summary>
/// Interface for customer management operations on payout providers.
/// </summary>
public interface ICustomerProvider
{
    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    PayoutProvider ProviderId { get; }

    /// <summary>
    /// Creates a customer in the provider's system.
    /// </summary>
    /// <param name="request">Customer creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provider-specific customer ID.</returns>
    Task<string> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a customer in the provider's system.
    /// </summary>
    /// <param name="providerCustomerId">Provider-specific customer ID.</param>
    /// <param name="request">Customer update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateCustomerAsync(string providerCustomerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customer details from the provider.
    /// </summary>
    /// <param name="providerCustomerId">Provider-specific customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Customer details from the provider.</returns>
    Task<Customer> GetCustomerAsync(string providerCustomerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a bank account to a customer in the provider's system.
    /// </summary>
    /// <param name="providerCustomerId">Provider-specific customer ID.</param>
    /// <param name="bankAccount">Bank account details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provider-specific bank account ID.</returns>
    Task<string> AddBankAccountAsync(string providerCustomerId, BankAccount bankAccount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates KYC verification for an individual customer.
    /// </summary>
    /// <param name="providerCustomerId">Provider-specific customer ID.</param>
    /// <param name="request">KYC initiation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification initiation result.</returns>
    Task<VerificationInitiationResult> InitiateKycAsync(string providerCustomerId, InitiateKycRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates KYB verification for a business customer.
    /// </summary>
    /// <param name="providerCustomerId">Provider-specific customer ID.</param>
    /// <param name="request">KYB initiation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification initiation result.</returns>
    Task<VerificationInitiationResult> InitiateKybAsync(string providerCustomerId, InitiateKybRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current verification status for a customer.
    /// </summary>
    /// <param name="providerCustomerId">Provider-specific customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification status result.</returns>
    Task<VerificationStatusResult> GetVerificationStatusAsync(string providerCustomerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a verification document.
    /// </summary>
    /// <param name="providerCustomerId">Provider-specific customer ID.</param>
    /// <param name="request">Document upload request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Document upload result.</returns>
    Task<DocumentUploadResult> UploadDocumentAsync(string providerCustomerId, UploadDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits verification for review.
    /// </summary>
    /// <param name="providerCustomerId">Provider-specific customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification status after submission.</returns>
    Task<VerificationStatusResult> SubmitVerificationAsync(string providerCustomerId, CancellationToken cancellationToken = default);
}
