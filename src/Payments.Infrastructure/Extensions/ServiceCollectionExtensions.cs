using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Core.Interfaces;
using Payments.Infrastructure.Configuration;
using Payments.Infrastructure.Providers.Borderless;
using Payments.Infrastructure.Providers.Mesta;
using Payments.Infrastructure.Services;
using Polly;
using Polly.Extensions.Http;

namespace Payments.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring payment services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds unified payment services to the service collection.
    /// This registers all payment providers (Mesta, Borderless) and services
    /// for customer management, KYC/KYB, and stablecoin payouts.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPayoutServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<PayoutSettings>(configuration.GetSection(PayoutSettings.SectionName));
        services.Configure<MestaSettings>(configuration.GetSection(MestaSettings.SectionName));
        services.Configure<BorderlessSettings>(configuration.GetSection(BorderlessSettings.SectionName));

        // Get settings for conditional registration
        var mestaSettings = configuration.GetSection(MestaSettings.SectionName).Get<MestaSettings>();
        var borderlessSettings = configuration.GetSection(BorderlessSettings.SectionName).Get<BorderlessSettings>();

        // Register Mesta provider if enabled
        if (mestaSettings?.Enabled == true)
        {
            services.AddMestaPaymentProvider(mestaSettings);
        }

        // Register Borderless provider if enabled
        if (borderlessSettings?.Enabled == true)
        {
            services.AddBorderlessPaymentProvider(borderlessSettings);
        }

        // Register unified services
        services.AddSingleton<PaymentProviderFactory>();
        services.AddScoped<UnifiedPaymentService>();
        services.AddScoped<ApiVerificationService>();

        return services;
    }

    /// <summary>
    /// Adds the Mesta payment provider with full support for customers, KYC/KYB, and payouts.
    /// </summary>
    private static IServiceCollection AddMestaPaymentProvider(
        this IServiceCollection services,
        MestaSettings settings)
    {
        services.AddHttpClient<MestaPaymentProvider>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(settings.RetryAttempts))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddSingleton<IPaymentProvider, MestaPaymentProvider>();

        return services;
    }

    /// <summary>
    /// Adds the Borderless payment provider with full support for customers, KYC/KYB, and payouts.
    /// </summary>
    private static IServiceCollection AddBorderlessPaymentProvider(
        this IServiceCollection services,
        BorderlessSettings settings)
    {
        services.AddHttpClient<BorderlessPaymentProvider>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(settings.RetryAttempts))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddSingleton<IPaymentProvider, BorderlessPaymentProvider>();

        return services;
    }

    /// <summary>
    /// Creates a retry policy for HTTP requests.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log retry if needed
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy for HTTP requests.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
