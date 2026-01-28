using Payments.Core.Enums;

namespace Payments.Core.Models.Responses;

/// <summary>
/// Result of a customer operation.
/// </summary>
public sealed class CustomerResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The customer (if successful).
    /// </summary>
    public Customer? Customer { get; init; }

    /// <summary>
    /// Provider that processed the request.
    /// </summary>
    public PayoutProvider? Provider { get; init; }

    /// <summary>
    /// Provider-specific customer ID.
    /// </summary>
    public string? ProviderCustomerId { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CustomerResult Succeeded(Customer customer, PayoutProvider provider, string? providerCustomerId = null) => new()
    {
        Success = true,
        Customer = customer,
        Provider = provider,
        ProviderCustomerId = providerCustomerId ?? customer.Id
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static CustomerResult Failed(string errorCode, string errorMessage, PayoutProvider? provider = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Provider = provider
    };
}

/// <summary>
/// Result of a customer list operation.
/// </summary>
public sealed class CustomerListResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// List of customers.
    /// </summary>
    public IReadOnlyList<Customer> Customers { get; init; } = [];

    /// <summary>
    /// Total count of customers matching the filter.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Provider that processed the request.
    /// </summary>
    public PayoutProvider? Provider { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static CustomerListResult Succeeded(
        IReadOnlyList<Customer> customers,
        int totalCount,
        int page,
        int pageSize,
        PayoutProvider provider) => new()
    {
        Success = true,
        Customers = customers,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        Provider = provider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static CustomerListResult Failed(string errorCode, string errorMessage, PayoutProvider? provider = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Provider = provider
    };
}

/// <summary>
/// Result of a document upload operation.
/// </summary>
public sealed class DocumentUploadResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Document ID assigned by the provider.
    /// </summary>
    public string? DocumentId { get; init; }

    /// <summary>
    /// Document type.
    /// </summary>
    public DocumentType? DocumentType { get; init; }

    /// <summary>
    /// Document status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Provider that processed the request.
    /// </summary>
    public PayoutProvider? Provider { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DocumentUploadResult Succeeded(
        string documentId,
        DocumentType documentType,
        string status,
        PayoutProvider provider) => new()
    {
        Success = true,
        DocumentId = documentId,
        DocumentType = documentType,
        Status = status,
        Provider = provider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static DocumentUploadResult Failed(string errorCode, string errorMessage, PayoutProvider? provider = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Provider = provider
    };
}

/// <summary>
/// Result of a document list operation.
/// </summary>
public sealed class DocumentListResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// List of documents.
    /// </summary>
    public IReadOnlyList<VerificationDocument> Documents { get; init; } = [];

    /// <summary>
    /// Provider that processed the request.
    /// </summary>
    public PayoutProvider? Provider { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DocumentListResult Succeeded(
        IReadOnlyList<VerificationDocument> documents,
        PayoutProvider provider) => new()
    {
        Success = true,
        Documents = documents,
        Provider = provider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static DocumentListResult Failed(string errorCode, string errorMessage, PayoutProvider? provider = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Provider = provider
    };
}
