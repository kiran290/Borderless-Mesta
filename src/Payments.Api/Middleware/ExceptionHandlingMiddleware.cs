using System.Text.Json;
using Payments.Api.Dtos;
using Payments.Core.Exceptions;

namespace Payments.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions and returning consistent error responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception occurred [RequestId: {RequestId}]",
            requestId);

        var (statusCode, response) = exception switch
        {
            PayoutValidationException validationEx => (
                StatusCodes.Status400BadRequest,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = validationEx.ErrorCode,
                        Message = validationEx.Message,
                        ValidationErrors = validationEx.ValidationErrors.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value)
                    },
                    RequestId = requestId
                }),

            ProviderNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = notFoundEx.ErrorCode,
                        Message = notFoundEx.Message
                    },
                    RequestId = requestId
                }),

            PayoutNotFoundException payoutNotFoundEx => (
                StatusCodes.Status404NotFound,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = payoutNotFoundEx.ErrorCode,
                        Message = payoutNotFoundEx.Message
                    },
                    RequestId = requestId
                }),

            QuoteExpiredException quoteExpiredEx => (
                StatusCodes.Status400BadRequest,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = quoteExpiredEx.ErrorCode,
                        Message = quoteExpiredEx.Message
                    },
                    RequestId = requestId
                }),

            UnsupportedConfigurationException unsupportedEx => (
                StatusCodes.Status400BadRequest,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = unsupportedEx.ErrorCode,
                        Message = unsupportedEx.Message
                    },
                    RequestId = requestId
                }),

            ProviderUnavailableException unavailableEx => (
                StatusCodes.Status503ServiceUnavailable,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = unavailableEx.ErrorCode,
                        Message = unavailableEx.Message
                    },
                    RequestId = requestId
                }),

            ProviderAuthenticationException authEx => (
                StatusCodes.Status502BadGateway,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = authEx.ErrorCode,
                        Message = "Provider authentication failed. Please try again later."
                    },
                    RequestId = requestId
                }),

            ProviderApiException apiEx => (
                StatusCodes.Status502BadGateway,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = apiEx.ErrorCode,
                        Message = apiEx.ProviderErrorMessage ?? "Provider returned an error. Please try again later."
                    },
                    RequestId = requestId
                }),

            PayoutCancellationException cancellationEx => (
                StatusCodes.Status400BadRequest,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = cancellationEx.ErrorCode,
                        Message = cancellationEx.Message
                    },
                    RequestId = requestId
                }),

            PayoutException payoutEx => (
                StatusCodes.Status400BadRequest,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = payoutEx.ErrorCode,
                        Message = payoutEx.Message
                    },
                    RequestId = requestId
                }),

            OperationCanceledException => (
                StatusCodes.Status499ClientClosedRequest,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "REQUEST_CANCELLED",
                        Message = "Request was cancelled"
                    },
                    RequestId = requestId
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ApiResponse<object>
                {
                    Success = false,
                    Error = new ApiError
                    {
                        Code = "INTERNAL_ERROR",
                        Message = "An unexpected error occurred. Please try again later."
                    },
                    RequestId = requestId
                })
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, _jsonOptions));
    }
}

/// <summary>
/// Extension methods for adding exception handling middleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
