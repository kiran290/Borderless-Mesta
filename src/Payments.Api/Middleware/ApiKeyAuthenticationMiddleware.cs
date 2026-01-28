using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Payments.Api.Dtos;

namespace Payments.Api.Middleware;

/// <summary>
/// Configuration for API key authentication.
/// </summary>
public sealed class ApiKeyAuthenticationOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Authentication:ApiKey";

    /// <summary>
    /// Header name for the API key.
    /// </summary>
    public string HeaderName { get; init; } = "X-API-Key";

    /// <summary>
    /// Alternative header name (for backwards compatibility).
    /// </summary>
    public string? AlternativeHeaderName { get; init; } = "Api-Key";

    /// <summary>
    /// Query parameter name for the API key (optional).
    /// </summary>
    public string? QueryParameterName { get; init; }

    /// <summary>
    /// Valid API keys and their associated client information.
    /// </summary>
    public Dictionary<string, ApiClientInfo> ApiKeys { get; init; } = new();

    /// <summary>
    /// Endpoints that don't require authentication.
    /// </summary>
    public List<string> ExcludedPaths { get; init; } = new()
    {
        "/health",
        "/swagger",
        "/api/v1/webhooks"
    };

    /// <summary>
    /// Enable authentication (can be disabled for development).
    /// </summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// Information about an API client.
/// </summary>
public sealed class ApiClientInfo
{
    /// <summary>
    /// Client name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Client ID.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Allowed scopes/permissions.
    /// </summary>
    public List<string> Scopes { get; init; } = new() { "payouts:read", "payouts:write", "customers:read", "customers:write", "kyc:read", "kyc:write" };

    /// <summary>
    /// Rate limit per minute.
    /// </summary>
    public int RateLimitPerMinute { get; init; } = 100;

    /// <summary>
    /// Whether the client is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Middleware for API key authentication.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeyAuthenticationOptions _options;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IOptions<ApiKeyAuthenticationOptions> options,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication if disabled
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Check if path is excluded
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (_options.ExcludedPaths.Any(p => path.StartsWith(p.ToLowerInvariant())))
        {
            await _next(context);
            return;
        }

        // Extract API key from request
        var apiKey = ExtractApiKey(context.Request);

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Missing API key for request: {Path}", context.Request.Path);
            await WriteUnauthorizedResponse(context, "API_KEY_MISSING", "API key is required");
            return;
        }

        // Validate API key
        if (!_options.ApiKeys.TryGetValue(apiKey, out var clientInfo))
        {
            _logger.LogWarning("Invalid API key used: {ApiKeyPrefix}...", apiKey[..Math.Min(8, apiKey.Length)]);
            await WriteUnauthorizedResponse(context, "API_KEY_INVALID", "Invalid API key");
            return;
        }

        // Check if client is active
        if (!clientInfo.IsActive)
        {
            _logger.LogWarning("Inactive API key used: {ClientId}", clientInfo.ClientId);
            await WriteUnauthorizedResponse(context, "API_KEY_INACTIVE", "API key is inactive");
            return;
        }

        // Set user identity
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, clientInfo.Name),
            new(ClaimTypes.NameIdentifier, clientInfo.ClientId),
            new("client_id", clientInfo.ClientId)
        };

        foreach (var scope in clientInfo.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, "ApiKey");
        context.User = new ClaimsPrincipal(identity);

        // Add client info to request items
        context.Items["ClientInfo"] = clientInfo;

        _logger.LogDebug("Authenticated request from client: {ClientId}", clientInfo.ClientId);

        await _next(context);
    }

    private string? ExtractApiKey(HttpRequest request)
    {
        // Try primary header
        if (request.Headers.TryGetValue(_options.HeaderName, out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }

        // Try alternative header
        if (!string.IsNullOrEmpty(_options.AlternativeHeaderName) &&
            request.Headers.TryGetValue(_options.AlternativeHeaderName, out var altHeaderValue))
        {
            return altHeaderValue.FirstOrDefault();
        }

        // Try query parameter
        if (!string.IsNullOrEmpty(_options.QueryParameterName) &&
            request.Query.TryGetValue(_options.QueryParameterName, out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        // Try Authorization header with ApiKey scheme
        if (request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.FirstOrDefault();
            if (authValue?.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase) == true)
            {
                return authValue["ApiKey ".Length..].Trim();
            }
            if (authValue?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                return authValue["Bearer ".Length..].Trim();
            }
        }

        return null;
    }

    private async Task WriteUnauthorizedResponse(HttpContext context, string errorCode, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            Error = new ApiError
            {
                Code = errorCode,
                Message = message
            },
            RequestId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}

/// <summary>
/// Extension methods for API key authentication.
/// </summary>
public static class ApiKeyAuthenticationExtensions
{
    /// <summary>
    /// Adds API key authentication to the application.
    /// </summary>
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }

    /// <summary>
    /// Adds API key authentication services.
    /// </summary>
    public static IServiceCollection AddApiKeyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ApiKeyAuthenticationOptions>(
            configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));

        return services;
    }
}

/// <summary>
/// Authorization attribute for requiring specific scopes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireScopeAttribute : Attribute
{
    public string[] Scopes { get; }

    public RequireScopeAttribute(params string[] scopes)
    {
        Scopes = scopes;
    }
}

/// <summary>
/// Authorization filter for scope-based authorization.
/// </summary>
public sealed class ScopeAuthorizationFilter : IEndpointFilter
{
    private readonly string[] _requiredScopes;

    public ScopeAuthorizationFilter(string[] requiredScopes)
    {
        _requiredScopes = requiredScopes;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Results.Unauthorized();
        }

        var userScopes = user.Claims
            .Where(c => c.Type == "scope")
            .Select(c => c.Value)
            .ToHashSet();

        if (!_requiredScopes.Any(s => userScopes.Contains(s)))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
