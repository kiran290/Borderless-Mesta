using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Core.Interfaces;
using Payments.Core.Models.Requests;

namespace Payments.Api.Controllers;

/// <summary>
/// Controller for KYC and KYB verification operations.
/// </summary>
[ApiController]
[Route("api/v1/verification")]
[Produces("application/json")]
public sealed class VerificationController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<VerificationController> _logger;

    public VerificationController(
        ICustomerService customerService,
        ILogger<VerificationController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    #region KYC Endpoints

    /// <summary>
    /// Initiates KYC verification for an individual customer.
    /// </summary>
    /// <param name="request">KYC initiation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification session details with redirect URL.</returns>
    [HttpPost("kyc/initiate")]
    [ProducesResponseType(typeof(ApiResponse<VerificationInitiationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiateKyc(
        [FromBody] InitiateKycRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Initiating KYC for customer: {CustomerId} [RequestId: {RequestId}]",
            request.CustomerId,
            requestId);

        var kycRequest = new InitiateKycRequest
        {
            CustomerId = request.CustomerId,
            TargetLevel = request.TargetLevel,
            RedirectUrl = request.RedirectUrl,
            WebhookUrl = request.WebhookUrl,
            PreferredProvider = request.PreferredProvider
        };

        var result = await _customerService.InitiateKycAsync(kycRequest, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(ApiResponse<object>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage ?? $"Customer '{request.CustomerId}' was not found",
                    requestId));
            }

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "KYC_INITIATION_FAILED",
                result.ErrorMessage ?? "Failed to initiate KYC verification",
                requestId));
        }

        var response = VerificationInitiationResponseDto.FromResult(result);

        _logger.LogInformation(
            "KYC initiated: SessionId={SessionId}, CustomerId={CustomerId} [RequestId: {RequestId}]",
            result.SessionId,
            request.CustomerId,
            requestId);

        return Ok(ApiResponse<VerificationInitiationResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets the current KYC status for a customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current KYC verification status.</returns>
    [HttpGet("kyc/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<VerificationStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKycStatus(string customerId, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Getting KYC status for customer: {CustomerId} [RequestId: {RequestId}]",
            customerId,
            requestId);

        var result = await _customerService.GetKycStatusAsync(customerId, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(ApiResponse<object>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage ?? $"Customer '{customerId}' was not found",
                    requestId));
            }

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "KYC_STATUS_FAILED",
                result.ErrorMessage ?? "Failed to get KYC status",
                requestId));
        }

        var response = VerificationStatusResponseDto.FromResult(result);
        return Ok(ApiResponse<VerificationStatusResponseDto>.Ok(response, requestId));
    }

    #endregion

    #region KYB Endpoints

    /// <summary>
    /// Initiates KYB verification for a business customer.
    /// </summary>
    /// <param name="request">KYB initiation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification session details with redirect URL.</returns>
    [HttpPost("kyb/initiate")]
    [ProducesResponseType(typeof(ApiResponse<VerificationInitiationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiateKyb(
        [FromBody] InitiateKybRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Initiating KYB for customer: {CustomerId} [RequestId: {RequestId}]",
            request.CustomerId,
            requestId);

        var kybRequest = new InitiateKybRequest
        {
            CustomerId = request.CustomerId,
            TargetLevel = request.TargetLevel,
            RedirectUrl = request.RedirectUrl,
            WebhookUrl = request.WebhookUrl,
            PreferredProvider = request.PreferredProvider
        };

        var result = await _customerService.InitiateKybAsync(kybRequest, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(ApiResponse<object>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage ?? $"Customer '{request.CustomerId}' was not found",
                    requestId));
            }

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "KYB_INITIATION_FAILED",
                result.ErrorMessage ?? "Failed to initiate KYB verification",
                requestId));
        }

        var response = VerificationInitiationResponseDto.FromResult(result);

        _logger.LogInformation(
            "KYB initiated: SessionId={SessionId}, CustomerId={CustomerId} [RequestId: {RequestId}]",
            result.SessionId,
            request.CustomerId,
            requestId);

        return Ok(ApiResponse<VerificationInitiationResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets the current KYB status for a business customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current KYB verification status.</returns>
    [HttpGet("kyb/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<VerificationStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKybStatus(string customerId, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Getting KYB status for customer: {CustomerId} [RequestId: {RequestId}]",
            customerId,
            requestId);

        var result = await _customerService.GetKybStatusAsync(customerId, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(ApiResponse<object>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage ?? $"Customer '{customerId}' was not found",
                    requestId));
            }

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "KYB_STATUS_FAILED",
                result.ErrorMessage ?? "Failed to get KYB status",
                requestId));
        }

        var response = VerificationStatusResponseDto.FromResult(result);
        return Ok(ApiResponse<VerificationStatusResponseDto>.Ok(response, requestId));
    }

    #endregion

    #region Document Endpoints

    /// <summary>
    /// Uploads a verification document for a customer.
    /// </summary>
    /// <param name="request">Document upload request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Upload result with document ID.</returns>
    [HttpPost("documents")]
    [ProducesResponseType(typeof(ApiResponse<DocumentUploadResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadDocument(
        [FromBody] UploadDocumentRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Uploading document for customer: {CustomerId}, Type: {DocumentType} [RequestId: {RequestId}]",
            request.CustomerId,
            request.DocumentType,
            requestId);

        var uploadRequest = new UploadDocumentRequest
        {
            CustomerId = request.CustomerId,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber,
            IssuingCountry = request.IssuingCountry,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            FrontImageBase64 = request.FrontImageBase64,
            BackImageBase64 = request.BackImageBase64,
            MimeType = request.MimeType
        };

        var result = await _customerService.UploadDocumentAsync(uploadRequest, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(ApiResponse<object>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage ?? $"Customer '{request.CustomerId}' was not found",
                    requestId));
            }

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "DOCUMENT_UPLOAD_FAILED",
                result.ErrorMessage ?? "Failed to upload document",
                requestId));
        }

        var response = DocumentUploadResponseDto.FromResult(result);

        _logger.LogInformation(
            "Document uploaded: DocumentId={DocumentId}, CustomerId={CustomerId} [RequestId: {RequestId}]",
            result.DocumentId,
            request.CustomerId,
            requestId);

        return Ok(ApiResponse<DocumentUploadResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets documents for a customer.
    /// </summary>
    /// <param name="customerId">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of verification documents.</returns>
    [HttpGet("documents/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VerificationDocumentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocuments(string customerId, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Getting documents for customer: {CustomerId} [RequestId: {RequestId}]",
            customerId,
            requestId);

        var documents = await _customerService.GetDocumentsAsync(customerId, cancellationToken);

        var response = documents.Select(d => new VerificationDocumentDto
        {
            Id = d.Id,
            Type = d.Type,
            Status = d.Status,
            DocumentNumber = d.DocumentNumber,
            IssuingCountry = d.IssuingCountry,
            IssueDate = d.IssueDate,
            ExpiryDate = d.ExpiryDate,
            RejectionReason = d.RejectionReason,
            UploadedAt = d.UploadedAt,
            VerifiedAt = d.VerifiedAt
        }).ToList();

        return Ok(ApiResponse<IReadOnlyList<VerificationDocumentDto>>.Ok(response, requestId));
    }

    /// <summary>
    /// Submits verification for review.
    /// </summary>
    /// <param name="request">Submission request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification status after submission.</returns>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(ApiResponse<VerificationStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitVerification(
        [FromBody] SubmitVerificationRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Submitting verification for customer: {CustomerId} [RequestId: {RequestId}]",
            request.CustomerId,
            requestId);

        var submitRequest = new SubmitVerificationRequest
        {
            CustomerId = request.CustomerId,
            AcceptDeclaration = request.AcceptDeclaration,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.FirstOrDefault()
        };

        var result = await _customerService.SubmitVerificationAsync(submitRequest, cancellationToken);

        if (!result.Success)
        {
            if (result.ErrorCode == "CUSTOMER_NOT_FOUND")
            {
                return NotFound(ApiResponse<object>.Fail(
                    result.ErrorCode,
                    result.ErrorMessage ?? $"Customer '{request.CustomerId}' was not found",
                    requestId));
            }

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "VERIFICATION_SUBMIT_FAILED",
                result.ErrorMessage ?? "Failed to submit verification",
                requestId));
        }

        var response = VerificationStatusResponseDto.FromResult(result);

        _logger.LogInformation(
            "Verification submitted: CustomerId={CustomerId}, Status={Status} [RequestId: {RequestId}]",
            request.CustomerId,
            result.Status,
            requestId);

        return Ok(ApiResponse<VerificationStatusResponseDto>.Ok(response, requestId));
    }

    #endregion
}
