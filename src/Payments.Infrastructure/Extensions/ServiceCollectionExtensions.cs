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
/// Extension methods for configuring payout services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds payout services to the service collection.
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
        var payoutSettings = configuration.GetSection(PayoutSettings.SectionName).Get<PayoutSettings>();
        var mestaSettings = configuration.GetSection(MestaSettings.SectionName).Get<MestaSettings>();
        var borderlessSettings = configuration.GetSection(BorderlessSettings.SectionName).Get<BorderlessSettings>();

        // Register Mesta provider if enabled
        if (mestaSettings?.Enabled == true)
        {
            services.AddMestaProvider(mestaSettings);
        }

        // Register Borderless provider if enabled
        if (borderlessSettings?.Enabled == true)
        {
            services.AddBorderlessProvider(borderlessSettings);
        }

        // Register core services
        services.AddSingleton<IPayoutProviderFactory, PayoutProviderFactory>();
        services.AddScoped<IPayoutService, PayoutService>();
        services.AddScoped<ICustomerService, CustomerService>();

        return services;
    }

    /// <summary>
    /// Adds the Mesta payout provider.
    /// </summary>
    private static IServiceCollection AddMestaProvider(
        this IServiceCollection services,
        MestaSettings settings)
    {
        services.AddHttpClient<MestaPayoutProvider>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(settings.RetryAttempts))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddHttpClient<MestaCustomerProvider>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(settings.RetryAttempts))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddSingleton<IPayoutProvider, MestaPayoutProvider>();
        services.AddSingleton<ICustomerProvider, MestaCustomerProvider>();
        services.AddSingleton<IWebhookHandler>(sp => sp.GetRequiredService<IPayoutProvider>() as MestaPayoutProvider
            ?? throw new InvalidOperationException("MestaPayoutProvider not registered"));

        return services;
    }

    /// <summary>
    /// Adds the Borderless payout provider.
    /// </summary>
    private static IServiceCollection AddBorderlessProvider(
        this IServiceCollection services,
        BorderlessSettings settings)
    {
        services.AddHttpClient<BorderlessPayoutProvider>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(settings.RetryAttempts))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddHttpClient<BorderlessCustomerProvider>(client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy(settings.RetryAttempts))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddSingleton<IPayoutProvider, BorderlessPayoutProvider>();
        services.AddSingleton<ICustomerProvider, BorderlessCustomerProvider>();
        services.AddSingleton<IWebhookHandler>(sp =>
        {
            var providers = sp.GetServices<IPayoutProvider>();
            return providers.OfType<BorderlessPayoutProvider>().First();
        });

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
