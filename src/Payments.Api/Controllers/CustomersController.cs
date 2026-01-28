using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Core.Enums;
using Payments.Core.Interfaces;
using Payments.Core.Models;
using Payments.Core.Models.Requests;

namespace Payments.Api.Controllers;

/// <summary>
/// Controller for customer onboarding and management operations.
/// </summary>
[ApiController]
[Route("api/v1/customers")]
[Produces("application/json")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerService customerService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="request">Customer creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created customer details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Creating customer: {Email}, Type: {Type} [RequestId: {RequestId}]",
            request.Contact.Email,
            request.Type,
            requestId);

        var createRequest = MapToCreateCustomerRequest(request);
        var result = await _customerService.CreateCustomerAsync(createRequest, cancellationToken);

        if (!result.Success || result.Customer == null)
        {
            _logger.LogWarning(
                "Customer creation failed: {ErrorCode} - {ErrorMessage} [RequestId: {RequestId}]",
                result.ErrorCode,
                result.ErrorMessage,
                requestId);

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "CUSTOMER_CREATE_FAILED",
                result.ErrorMessage ?? "Failed to create customer",
                requestId));
        }

        var response = CustomerResponseDto.FromModel(result.Customer);

        _logger.LogInformation(
            "Customer created: {CustomerId} [RequestId: {RequestId}]",
            result.Customer.Id,
            requestId);

        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = result.Customer.Id },
            ApiResponse<CustomerResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    /// <param name="id">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Customer details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(string id, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Getting customer: {CustomerId} [RequestId: {RequestId}]", id, requestId);

        var customer = await _customerService.GetCustomerAsync(id, cancellationToken);

        if (customer == null)
        {
            return NotFound(ApiResponse<object>.Fail(
                "CUSTOMER_NOT_FOUND",
                $"Customer '{id}' was not found",
                requestId));
        }

        var response = CustomerResponseDto.FromModel(customer);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets a customer by external ID.
    /// </summary>
    /// <param name="externalId">External reference ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Customer details.</returns>
    [HttpGet("external/{externalId}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerByExternalId(string externalId, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Getting customer by external ID: {ExternalId} [RequestId: {RequestId}]", externalId, requestId);

        var customer = await _customerService.GetCustomerByExternalIdAsync(externalId, cancellationToken);

        if (customer == null)
        {
            return NotFound(ApiResponse<object>.Fail(
                "CUSTOMER_NOT_FOUND",
                $"Customer with external ID '{externalId}' was not found",
                requestId));
        }

        var response = CustomerResponseDto.FromModel(customer);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Updates a customer.
    /// </summary>
    /// <param name="id">Customer ID.</param>
    /// <param name="request">Update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated customer details.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer(
        string id,
        [FromBody] UpdateCustomerRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Updating customer: {CustomerId} [RequestId: {RequestId}]", id, requestId);

        var updateRequest = MapToUpdateCustomerRequest(request);
        var result = await _customerService.UpdateCustomerAsync(id, updateRequest, cancellationToken);

        if (!result.Success || result.Customer == null)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(ApiResponse<object>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage ?? $"Customer '{id}' was not found",
                    requestId));
            }

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "CUSTOMER_UPDATE_FAILED",
                result.ErrorMessage ?? "Failed to update customer",
                requestId));
        }

        var response = CustomerResponseDto.FromModel(result.Customer);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Adds a bank account to a customer.
    /// </summary>
    /// <param name="id">Customer ID.</param>
    /// <param name="request">Bank account request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated customer details.</returns>
    [HttpPost("{id}/bank-accounts")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddBankAccount(
        string id,
        [FromBody] AddBankAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Adding bank account to customer: {CustomerId} [RequestId: {RequestId}]", id, requestId);

        var bankAccount = MapToBankAccount(request.BankAccount);
        var addRequest = new AddBankAccountRequest
        {
            BankAccount = bankAccount,
            SetAsPrimary = request.SetAsPrimary
        };

        var result = await _customerService.AddBankAccountAsync(id, addRequest, cancellationToken);

        if (!result.Success || result.Customer == null)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(ApiResponse<object>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage ?? $"Customer '{id}' was not found",
                    requestId));
            }

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "BANK_ACCOUNT_ADD_FAILED",
                result.ErrorMessage ?? "Failed to add bank account",
                requestId));
        }

        var response = CustomerResponseDto.FromModel(result.Customer);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(response, requestId));
    }

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
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCustomers(
        [FromQuery] CustomerType? type = null,
        [FromQuery] CustomerRole? role = null,
        [FromQuery] CustomerStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Listing customers: Type={Type}, Role={Role}, Status={Status} [RequestId: {RequestId}]",
            type,
            role,
            status,
            requestId);

        var customers = await _customerService.ListCustomersAsync(type, role, status, skip, take, cancellationToken);
        var response = customers.Select(CustomerResponseDto.FromModel).ToList();

        return Ok(ApiResponse<IReadOnlyList<CustomerResponseDto>>.Ok(response, requestId));
    }

    #region Private Helpers

    private static CreateCustomerRequest MapToCreateCustomerRequest(CreateCustomerRequestDto dto)
    {
        return new CreateCustomerRequest
        {
            ExternalId = dto.ExternalId,
            Type = dto.Type,
            Role = dto.Role,
            Individual = dto.Individual != null ? new IndividualDetails
            {
                FirstName = dto.Individual.FirstName,
                MiddleName = dto.Individual.MiddleName,
                LastName = dto.Individual.LastName,
                DateOfBirth = dto.Individual.DateOfBirth,
                CountryOfBirth = dto.Individual.CountryOfBirth,
                Nationality = dto.Individual.Nationality,
                Gender = dto.Individual.Gender,
                Occupation = dto.Individual.Occupation,
                TaxId = dto.Individual.TaxId
            } : null,
            Business = dto.Business != null ? new BusinessDetails
            {
                LegalName = dto.Business.LegalName,
                TradingName = dto.Business.TradingName,
                RegistrationNumber = dto.Business.RegistrationNumber,
                TaxId = dto.Business.TaxId,
                VatNumber = dto.Business.VatNumber,
                CountryOfIncorporation = dto.Business.CountryOfIncorporation,
                DateOfIncorporation = dto.Business.DateOfIncorporation,
                EntityType = dto.Business.EntityType,
                Industry = dto.Business.Industry,
                Website = dto.Business.Website,
                Description = dto.Business.Description,
                BeneficialOwners = dto.Business.BeneficialOwners?.Select(bo => new BeneficialOwner
                {
                    FirstName = bo.FirstName,
                    LastName = bo.LastName,
                    DateOfBirth = bo.DateOfBirth,
                    Nationality = bo.Nationality,
                    OwnershipPercentage = bo.OwnershipPercentage,
                    DocumentType = bo.DocumentType,
                    DocumentNumber = bo.DocumentNumber
                }).ToList() ?? new List<BeneficialOwner>(),
                Directors = dto.Business.Directors?.Select(d => new Director
                {
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    DateOfBirth = d.DateOfBirth,
                    Nationality = d.Nationality,
                    Title = d.Title,
                    AppointedDate = d.AppointedDate
                }).ToList() ?? new List<Director>()
            } : null,
            Contact = new ContactInfo
            {
                Email = dto.Contact.Email,
                Phone = dto.Contact.Phone,
                Mobile = dto.Contact.Mobile
            },
            Address = dto.Address != null ? new Address
            {
                Street1 = dto.Address.Street1,
                Street2 = dto.Address.Street2,
                City = dto.Address.City,
                State = dto.Address.State,
                PostalCode = dto.Address.PostalCode,
                CountryCode = dto.Address.CountryCode
            } : null,
            BankAccounts = dto.BankAccounts?.Select(ba => MapToBankAccount(ba)).ToList(),
            PreferredProvider = dto.PreferredProvider,
            Metadata = dto.Metadata
        };
    }

    private static UpdateCustomerRequest MapToUpdateCustomerRequest(UpdateCustomerRequestDto dto)
    {
        return new UpdateCustomerRequest
        {
            Individual = dto.Individual != null ? new IndividualDetails
            {
                FirstName = dto.Individual.FirstName,
                MiddleName = dto.Individual.MiddleName,
                LastName = dto.Individual.LastName,
                DateOfBirth = dto.Individual.DateOfBirth,
                CountryOfBirth = dto.Individual.CountryOfBirth,
                Nationality = dto.Individual.Nationality,
                Gender = dto.Individual.Gender,
                Occupation = dto.Individual.Occupation,
                TaxId = dto.Individual.TaxId
            } : null,
            Business = dto.Business != null ? new BusinessDetails
            {
                LegalName = dto.Business.LegalName,
                TradingName = dto.Business.TradingName,
                RegistrationNumber = dto.Business.RegistrationNumber,
                TaxId = dto.Business.TaxId,
                VatNumber = dto.Business.VatNumber,
                CountryOfIncorporation = dto.Business.CountryOfIncorporation,
                DateOfIncorporation = dto.Business.DateOfIncorporation,
                EntityType = dto.Business.EntityType,
                Industry = dto.Business.Industry,
                Website = dto.Business.Website,
                Description = dto.Business.Description
            } : null,
            Contact = dto.Contact != null ? new ContactInfo
            {
                Email = dto.Contact.Email,
                Phone = dto.Contact.Phone,
                Mobile = dto.Contact.Mobile
            } : null,
            Address = dto.Address != null ? new Address
            {
                Street1 = dto.Address.Street1,
                Street2 = dto.Address.Street2,
                City = dto.Address.City,
                State = dto.Address.State,
                PostalCode = dto.Address.PostalCode,
                CountryCode = dto.Address.CountryCode
            } : null,
            Metadata = dto.Metadata
        };
    }

    private static BankAccount MapToBankAccount(BankAccountDto dto)
    {
        return new BankAccount
        {
            BankName = dto.BankName,
            AccountNumber = dto.AccountNumber,
            AccountHolderName = dto.AccountHolderName,
            RoutingNumber = dto.RoutingNumber,
            SwiftCode = dto.SwiftCode,
            SortCode = dto.SortCode,
            Iban = dto.Iban,
            Currency = dto.Currency,
            CountryCode = dto.CountryCode,
            BranchCode = dto.BranchCode
        };
    }

    #endregion
}
