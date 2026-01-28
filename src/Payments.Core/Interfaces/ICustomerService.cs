using Payments.Core.Enums;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;

namespace Payments.Core.Interfaces;

/// <summary>
/// High-level service interface for customer management and verification operations.
/// </summary>
public interface ICustomerService
{
    #region Customer Management

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="request">Customer creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created customer result.</returns>
    Task<CustomerResult> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Customer details.</returns>
    Task<Customer?> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by external ID.
    /// </summary>
    /// <param name="externalId">External reference ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Customer details.</returns>
    Task<Customer?> GetCustomerByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated customer result.</returns>
    Task<CustomerResult> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a bank account to a customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="request">Bank account request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated customer result.</returns>
    Task<CustomerResult> AddBankAccountAsync(string customerId, AddBankAccountRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists customers with optional filters.
    /// </summary>
    /// <param name="type">Filter by customer type.</param>
    /// <param name="role">Filter by customer role.</param>
    /// <param name="status">Filter by customer status.</param>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of customers.</returns>
    Task<IReadOnlyList<Customer>> ListCustomersAsync(
        CustomerType? type = null,
        CustomerRole? role = null,
        CustomerStatus? status = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    #endregion

    #region KYC Operations

    /// <summary>
    /// Initiates KYC verification for an individual customer.
    /// </summary>
    /// <param name="request">KYC initiation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification initiation result.</returns>
    Task<VerificationInitiationResult> InitiateKycAsync(InitiateKycRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current KYC status for a customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification status result.</returns>
    Task<VerificationStatusResult> GetKycStatusAsync(string customerId, CancellationToken cancellationToken = default);

    #endregion

    #region KYB Operations

    /// <summary>
    /// Initiates KYB verification for a business customer.
    /// </summary>
    /// <param name="request">KYB initiation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification initiation result.</returns>
    Task<VerificationInitiationResult> InitiateKybAsync(InitiateKybRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current KYB status for a business customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification status result.</returns>
    Task<VerificationStatusResult> GetKybStatusAsync(string customerId, CancellationToken cancellationToken = default);

    #endregion

    #region Document Management

    /// <summary>
    /// Uploads a verification document.
    /// </summary>
    /// <param name="request">Document upload request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Document upload result.</returns>
    Task<DocumentUploadResult> UploadDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents for a customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of verification documents.</returns>
    Task<IReadOnlyList<VerificationDocument>> GetDocumentsAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits verification for review.
    /// </summary>
    /// <param name="request">Submission request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification status after submission.</returns>
    Task<VerificationStatusResult> SubmitVerificationAsync(SubmitVerificationRequest request, CancellationToken cancellationToken = default);

    #endregion
}
