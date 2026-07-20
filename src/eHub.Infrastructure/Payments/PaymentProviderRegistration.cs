using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Infrastructure.Payments.Providers.Stripe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eHub.Infrastructure.Payments;

internal static class PaymentProviderRegistration
{
    public static IServiceCollection AddPaymentProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<PaymentProviderOptions>(configuration.GetSection(PaymentProviderOptions.SectionName));

        var options = configuration.GetSection(PaymentProviderOptions.SectionName).Get<PaymentProviderOptions>()
            ?? new PaymentProviderOptions();

        if (environment.IsProduction() && options.Fake.Enabled)
        {
            throw new InvalidOperationException(
                "Payments:Providers:Fake.Enabled must be false in Production.");
        }

        if (!environment.IsProduction() && options.Fake.Enabled)
        {
            services.AddSingleton<IPaymentProvider, FakePaymentProvider>();
        }

        services.AddStripePaymentProvider(configuration, environment);

        if (options.Payriff.Enabled)
        {
            ValidateEnabledSecrets(
                PaymentProviderCodes.Payriff,
                options.Payriff.SecretKey,
                options.Payriff.WebhookSecret);
            services.AddSingleton<IPaymentProvider, PayriffPaymentProvider>();
        }

        services.AddSingleton<IPaymentProviderResolver, PaymentProviderResolver>();
        return services;
    }

    private static void ValidateEnabledSecrets(string providerKey, string primarySecret, string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(primarySecret) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new InvalidOperationException(
                $"Payments:Providers:{providerKey} is enabled but required secrets are missing. " +
                "Disable the provider or supply credentials before startup.");
        }
    }
}
