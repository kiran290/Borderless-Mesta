using System.Text.Json;
using Microsoft.Extensions.Options;
using StablecoinPayments.Api.Dtos;
using StablecoinPayments.Infrastructure.Configuration;

namespace StablecoinPayments.Api.Middleware;

public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthenticationSettings _settings;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(
        RequestDelegate next,
        IOptions<AuthenticationSettings> settings,
        ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_settings.ApiKey.Enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

        // Check excluded paths
        if (_settings.ApiKey.ExcludedPaths.Any(p => path.StartsWith(p.ToLower())))
        {
            await _next(context);
            return;
        }

        // Get API key from header
        if (!context.Request.Headers.TryGetValue(_settings.ApiKey.HeaderName, out var apiKeyValue))
        {
            _logger.LogWarning("API key missing from request to {Path}", path);
            await WriteUnauthorizedResponse(context, "API key is required");
            return;
        }

        var apiKey = apiKeyValue.ToString();

        // Validate API key
        if (!_settings.ApiKey.ApiKeys.TryGetValue(apiKey, out var keyConfig) || !keyConfig.IsActive)
        {
            _logger.LogWarning("Invalid API key used for request to {Path}", path);
            await WriteUnauthorizedResponse(context, "Invalid API key");
            return;
        }

        // Add client info to context
        context.Items["ClientId"] = keyConfig.ClientId;
        context.Items["ClientName"] = keyConfig.Name;
        context.Items["Scopes"] = keyConfig.Scopes;

        await _next(context);
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Error("UNAUTHORIZED", message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
