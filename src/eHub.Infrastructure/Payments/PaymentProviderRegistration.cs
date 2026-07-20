using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
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

        services.AddSingleton<IPaymentProvider, FakePaymentProvider>();

        if (options.Stripe.Enabled)
        {
            ValidateEnabledProviderSecrets(
                environment,
                PaymentProviderCodes.Stripe,
                options.Stripe.ApiKey,
                options.Stripe.WebhookSecret);
            services.AddSingleton<IPaymentProvider, StripePaymentProvider>();
        }

        if (options.Payriff.Enabled)
        {
            ValidateEnabledProviderSecrets(
                environment,
                PaymentProviderCodes.Payriff,
                options.Payriff.SecretKey,
                options.Payriff.WebhookSecret);
            services.AddSingleton<IPaymentProvider, PayriffPaymentProvider>();
        }

        services.AddSingleton<IPaymentProviderResolver, PaymentProviderResolver>();
        return services;
    }

    private static void ValidateEnabledProviderSecrets(
        IHostEnvironment environment,
        string providerKey,
        string primarySecret,
        string webhookSecret)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(primarySecret) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new InvalidOperationException(
                $"Payments:Providers:{providerKey} is enabled in Production but required secrets are missing. " +
                "Disable the provider or supply credentials before startup.");
        }
    }
}
