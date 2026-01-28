using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly;
using Polly.Extensions.Http;
using StablecoinPayments.Core.Interfaces;
using StablecoinPayments.Infrastructure.Configuration;
using StablecoinPayments.Infrastructure.Providers.Borderless;
using StablecoinPayments.Infrastructure.Providers.Mesta;
using StablecoinPayments.Infrastructure.Services;

namespace StablecoinPayments.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<PaymentSettings>(configuration.GetSection("Payment"));

        var settings = configuration.GetSection("Payment").Get<PaymentSettings>() ?? new PaymentSettings();

        // Register Mesta provider if enabled
        if (settings.Mesta.Enabled)
        {
            services.AddHttpClient<MestaPaymentProvider>("Mesta", client =>
            {
                client.BaseAddress = new Uri(settings.Mesta.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.Mesta.TimeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy(settings.Mesta.RetryAttempts))
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<MestaPaymentProvider>());
        }

        // Register Borderless provider if enabled
        if (settings.Borderless.Enabled)
        {
            services.AddHttpClient<BorderlessPaymentProvider>("Borderless", client =>
            {
                client.BaseAddress = new Uri(settings.Borderless.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.Borderless.TimeoutSeconds);
            })
            .AddPolicyHandler(GetRetryPolicy(settings.Borderless.RetryAttempts))
            .AddPolicyHandler(GetCircuitBreakerPolicy());

            services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<BorderlessPaymentProvider>());
        }

        // Register factory and services
        services.AddSingleton<PaymentProviderFactory>();
        services.AddScoped<UnifiedPaymentService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
