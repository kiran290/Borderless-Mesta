using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Core.Enums;
using Payments.Core.Exceptions;
using Payments.Core.Interfaces;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;
using Payments.Infrastructure.Configuration;

namespace Payments.Infrastructure.Services;

/// <summary>
/// High-level service for customer management and verification operations.
/// </summary>
public sealed class CustomerService : ICustomerService
{
    private readonly IReadOnlyDictionary<PayoutProvider, ICustomerProvider> _providers;
    private readonly PayoutSettings _settings;
    private readonly ILogger<CustomerService> _logger;

    // In-memory store for demo purposes - replace with database in production
    private readonly ConcurrentDictionary<string, CustomerRecord> _customers = new();
    private readonly ConcurrentDictionary<string, string> _externalIdIndex = new();

    public CustomerService(
        IEnumerable<ICustomerProvider> providers,
        IOptions<PayoutSettings> settings,
        ILogger<CustomerService> logger)
    {
        _providers = providers.ToDictionary(p => p.ProviderId, p => p);
        _settings = settings.Value;
        _logger = logger;

        _logger.LogInformation(
            "CustomerService initialized with {Count} providers: {Providers}",
            _providers.Count,
            string.Join(", ", _providers.Keys));
    }

    #region Customer Management

    public async Task<CustomerResult> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating customer: {Email}, Type: {Type}", request.Contact.Email, request.Type);

        try
        {
            var provider = GetProvider(request.PreferredProvider);
            var providerCustomerId = await provider.CreateCustomerAsync(request, cancellationToken);

            var customer = new Customer
            {
                Id = Guid.NewGuid().ToString(),
                ExternalId = request.ExternalId,
                Type = request.Type,
                Role = request.Role,
                Status = CustomerStatus.Pending,
                Individual = request.Individual,
                Business = request.Business,
                Contact = request.Contact,
                Address = request.Address,
                BankAccounts = request.BankAccounts ?? Array.Empty<BankAccount>(),
                ProviderIds = new Dictionary<PayoutProvider, string>
                {
                    { provider.ProviderId, providerCustomerId }
                },
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                Metadata = request.Metadata
            };

            var record = new CustomerRecord
            {
                Customer = customer,
                PrimaryProvider = provider.ProviderId
            };

            _customers[customer.Id] = record;

            if (!string.IsNullOrEmpty(request.ExternalId))
            {
                _externalIdIndex[request.ExternalId] = customer.Id;
            }

            _logger.LogInformation("Customer created: {CustomerId}, Provider: {Provider}", customer.Id, provider.ProviderId);
            return CustomerResult.Succeeded(customer);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to create customer");
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "CUSTOMER_CREATE_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public Task<Customer?> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting customer: {CustomerId}", customerId);

        if (_customers.TryGetValue(customerId, out var record))
        {
            return Task.FromResult<Customer?>(record.Customer);
        }

        return Task.FromResult<Customer?>(null);
    }

    public Task<Customer?> GetCustomerByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting customer by external ID: {ExternalId}", externalId);

        if (_externalIdIndex.TryGetValue(externalId, out var customerId))
        {
            return GetCustomerAsync(customerId, cancellationToken);
        }

        return Task.FromResult<Customer?>(null);
    }

    public async Task<CustomerResult> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating customer: {CustomerId}", customerId);

        if (!_customers.TryGetValue(customerId, out var record))
        {
            return CustomerResult.Failed("CUSTOMER_NOT_FOUND", $"Customer '{customerId}' was not found");
        }

        try
        {
            var provider = GetProvider(record.PrimaryProvider);
            var providerCustomerId = record.Customer.ProviderIds[record.PrimaryProvider];

            await provider.UpdateCustomerAsync(providerCustomerId, request, cancellationToken);

            // Update local record
            var updatedCustomer = record.Customer with
            {
                Individual = request.Individual ?? record.Customer.Individual,
                Business = request.Business ?? record.Customer.Business,
                Contact = request.Contact ?? record.Customer.Contact,
                Address = request.Address ?? record.Customer.Address,
                UpdatedAt = DateTimeOffset.UtcNow,
                Metadata = request.Metadata ?? record.Customer.Metadata
            };

            record.Customer = updatedCustomer;

            _logger.LogInformation("Customer updated: {CustomerId}", customerId);
            return CustomerResult.Succeeded(updatedCustomer);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to update customer: {CustomerId}", customerId);
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "CUSTOMER_UPDATE_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<CustomerResult> AddBankAccountAsync(string customerId, AddBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding bank account to customer: {CustomerId}", customerId);

        if (!_customers.TryGetValue(customerId, out var record))
        {
            return CustomerResult.Failed("CUSTOMER_NOT_FOUND", $"Customer '{customerId}' was not found");
        }

        try
        {
            var provider = GetProvider(record.PrimaryProvider);
            var providerCustomerId = record.Customer.ProviderIds[record.PrimaryProvider];

            await provider.AddBankAccountAsync(providerCustomerId, request.BankAccount, cancellationToken);

            // Update local record
            var bankAccounts = record.Customer.BankAccounts.ToList();
            bankAccounts.Add(request.BankAccount);

            var updatedCustomer = record.Customer with
            {
                BankAccounts = bankAccounts,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            record.Customer = updatedCustomer;

            _logger.LogInformation("Bank account added to customer: {CustomerId}", customerId);
            return CustomerResult.Succeeded(updatedCustomer);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to add bank account: {CustomerId}", customerId);
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "BANK_ACCOUNT_ADD_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public Task<IReadOnlyList<Customer>> ListCustomersAsync(
        CustomerType? type = null,
        CustomerRole? role = null,
        CustomerStatus? status = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing customers: Type={Type}, Role={Role}, Status={Status}", type, role, status);

        var query = _customers.Values.AsEnumerable();

        if (type.HasValue)
        {
            query = query.Where(r => r.Customer.Type == type.Value);
        }

        if (role.HasValue)
        {
            query = query.Where(r => r.Customer.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Customer.Status == status.Value);
        }

        var customers = query
            .OrderByDescending(r => r.Customer.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(r => r.Customer)
            .ToList();

        return Task.FromResult<IReadOnlyList<Customer>>(customers);
    }

    #endregion

    #region KYC Operations

    public async Task<VerificationInitiationResult> InitiateKycAsync(InitiateKycRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYC for customer: {CustomerId}", request.CustomerId);

        if (!_customers.TryGetValue(request.CustomerId, out var record))
        {
            return VerificationInitiationResult.Failed("CUSTOMER_NOT_FOUND", $"Customer '{request.CustomerId}' was not found");
        }

        if (record.Customer.Type != CustomerType.Individual)
        {
            return VerificationInitiationResult.Failed("INVALID_CUSTOMER_TYPE", "KYC is only available for individual customers");
        }

        try
        {
            var provider = GetProvider(request.PreferredProvider ?? record.PrimaryProvider);
            var providerCustomerId = record.Customer.ProviderIds.GetValueOrDefault(provider.ProviderId);

            if (string.IsNullOrEmpty(providerCustomerId))
            {
                // Create customer in this provider first
                var createRequest = new CreateCustomerRequest
                {
                    Type = record.Customer.Type,
                    Role = record.Customer.Role,
                    Individual = record.Customer.Individual,
                    Contact = record.Customer.Contact,
                    Address = record.Customer.Address
                };
                providerCustomerId = await provider.CreateCustomerAsync(createRequest, cancellationToken);
                record.Customer.ProviderIds[provider.ProviderId] = providerCustomerId;
            }

            var result = await provider.InitiateKycAsync(providerCustomerId, request, cancellationToken);

            if (result.Success)
            {
                // Update verification status
                var verification = new VerificationInfo
                {
                    Status = VerificationStatus.Pending,
                    Level = VerificationLevel.None,
                    ProviderVerificationIds = new Dictionary<PayoutProvider, string>
                    {
                        { provider.ProviderId, result.SessionId! }
                    }
                };

                record.Customer = record.Customer with
                {
                    Verification = verification,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }

            return result;
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to initiate KYC: {CustomerId}", request.CustomerId);
            return VerificationInitiationResult.Failed(ex.ProviderErrorCode ?? "KYC_INITIATION_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<VerificationStatusResult> GetKycStatusAsync(string customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting KYC status for customer: {CustomerId}", customerId);

        if (!_customers.TryGetValue(customerId, out var record))
        {
            return VerificationStatusResult.Failed("CUSTOMER_NOT_FOUND", $"Customer '{customerId}' was not found");
        }

        try
        {
            var provider = GetProvider(record.PrimaryProvider);
            var providerCustomerId = record.Customer.ProviderIds[record.PrimaryProvider];

            var result = await provider.GetVerificationStatusAsync(providerCustomerId, cancellationToken);

            if (result.Success && result.Verification != null)
            {
                // Update local verification status
                record.Customer = record.Customer with
                {
                    Verification = result.Verification,
                    Status = result.Verification.Status == VerificationStatus.Approved
                        ? CustomerStatus.Active
                        : record.Customer.Status,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }

            return result;
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get KYC status: {CustomerId}", customerId);
            return VerificationStatusResult.Failed(ex.ProviderErrorCode ?? "KYC_STATUS_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    #endregion

    #region KYB Operations

    public async Task<VerificationInitiationResult> InitiateKybAsync(InitiateKybRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYB for customer: {CustomerId}", request.CustomerId);

        if (!_customers.TryGetValue(request.CustomerId, out var record))
        {
            return VerificationInitiationResult.Failed("CUSTOMER_NOT_FOUND", $"Customer '{request.CustomerId}' was not found");
        }

        if (record.Customer.Type != CustomerType.Business)
        {
            return VerificationInitiationResult.Failed("INVALID_CUSTOMER_TYPE", "KYB is only available for business customers");
        }

        try
        {
            var provider = GetProvider(request.PreferredProvider ?? record.PrimaryProvider);
            var providerCustomerId = record.Customer.ProviderIds.GetValueOrDefault(provider.ProviderId);

            if (string.IsNullOrEmpty(providerCustomerId))
            {
                // Create customer in this provider first
                var createRequest = new CreateCustomerRequest
                {
                    Type = record.Customer.Type,
                    Role = record.Customer.Role,
                    Business = record.Customer.Business,
                    Contact = record.Customer.Contact,
                    Address = record.Customer.Address
                };
                providerCustomerId = await provider.CreateCustomerAsync(createRequest, cancellationToken);
                record.Customer.ProviderIds[provider.ProviderId] = providerCustomerId;
            }

            var result = await provider.InitiateKybAsync(providerCustomerId, request, cancellationToken);

            if (result.Success)
            {
                // Update verification status
                var verification = new VerificationInfo
                {
                    Status = VerificationStatus.Pending,
                    Level = VerificationLevel.None,
                    ProviderVerificationIds = new Dictionary<PayoutProvider, string>
                    {
                        { provider.ProviderId, result.SessionId! }
                    }
                };

                record.Customer = record.Customer with
                {
                    Verification = verification,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }

            return result;
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to initiate KYB: {CustomerId}", request.CustomerId);
            return VerificationInitiationResult.Failed(ex.ProviderErrorCode ?? "KYB_INITIATION_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<VerificationStatusResult> GetKybStatusAsync(string customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting KYB status for customer: {CustomerId}", customerId);

        if (!_customers.TryGetValue(customerId, out var record))
        {
            return VerificationStatusResult.Failed("CUSTOMER_NOT_FOUND", $"Customer '{customerId}' was not found");
        }

        try
        {
            var provider = GetProvider(record.PrimaryProvider);
            var providerCustomerId = record.Customer.ProviderIds[record.PrimaryProvider];

            var result = await provider.GetVerificationStatusAsync(providerCustomerId, cancellationToken);

            if (result.Success && result.Verification != null)
            {
                // Update local verification status
                record.Customer = record.Customer with
                {
                    Verification = result.Verification,
                    Status = result.Verification.Status == VerificationStatus.Approved
                        ? CustomerStatus.Active
                        : record.Customer.Status,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }

            return result;
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get KYB status: {CustomerId}", customerId);
            return VerificationStatusResult.Failed(ex.ProviderErrorCode ?? "KYB_STATUS_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    #endregion

    #region Document Management

    public async Task<DocumentUploadResult> UploadDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading document for customer: {CustomerId}, Type: {DocumentType}", request.CustomerId, request.DocumentType);

        if (!_customers.TryGetValue(request.CustomerId, out var record))
        {
            return DocumentUploadResult.Failed("CUSTOMER_NOT_FOUND", $"Customer '{request.CustomerId}' was not found");
        }

        try
        {
            var provider = GetProvider(record.PrimaryProvider);
            var providerCustomerId = record.Customer.ProviderIds[record.PrimaryProvider];

            return await provider.UploadDocumentAsync(providerCustomerId, request, cancellationToken);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to upload document: {CustomerId}", request.CustomerId);
            return DocumentUploadResult.Failed(ex.ProviderErrorCode ?? "DOCUMENT_UPLOAD_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public Task<IReadOnlyList<VerificationDocument>> GetDocumentsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting documents for customer: {CustomerId}", customerId);

        if (!_customers.TryGetValue(customerId, out var record))
        {
            return Task.FromResult<IReadOnlyList<VerificationDocument>>(Array.Empty<VerificationDocument>());
        }

        return Task.FromResult(record.Customer.Verification?.Documents ?? Array.Empty<VerificationDocument>());
    }

    public async Task<VerificationStatusResult> SubmitVerificationAsync(SubmitVerificationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting verification for customer: {CustomerId}", request.CustomerId);

        if (!_customers.TryGetValue(request.CustomerId, out var record))
        {
            return VerificationStatusResult.Failed("CUSTOMER_NOT_FOUND", $"Customer '{request.CustomerId}' was not found");
        }

        if (!request.AcceptDeclaration)
        {
            return VerificationStatusResult.Failed("DECLARATION_NOT_ACCEPTED", "You must accept the declaration to submit verification");
        }

        try
        {
            var provider = GetProvider(record.PrimaryProvider);
            var providerCustomerId = record.Customer.ProviderIds[record.PrimaryProvider];

            var result = await provider.SubmitVerificationAsync(providerCustomerId, cancellationToken);

            if (result.Success && result.Verification != null)
            {
                record.Customer = record.Customer with
                {
                    Verification = result.Verification,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
            }

            return result;
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to submit verification: {CustomerId}", request.CustomerId);
            return VerificationStatusResult.Failed(ex.ProviderErrorCode ?? "VERIFICATION_SUBMIT_FAILED", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    #endregion

    #region Private Helpers

    private ICustomerProvider GetProvider(PayoutProvider? preferredProvider = null)
    {
        var providerId = preferredProvider ?? _settings.DefaultProvider;

        if (!_providers.TryGetValue(providerId, out var provider))
        {
            throw new ProviderNotFoundException(providerId);
        }

        return provider;
    }

    private sealed class CustomerRecord
    {
        public required Customer Customer { get; set; }
        public required PayoutProvider PrimaryProvider { get; init; }
    }

    #endregion
}
