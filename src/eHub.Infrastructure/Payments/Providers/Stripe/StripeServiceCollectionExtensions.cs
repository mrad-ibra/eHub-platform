using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

namespace eHub.Infrastructure.Payments.Providers.Stripe;

public static class StripeServiceCollectionExtensions
{
    public static IServiceCollection AddStripePaymentProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(PaymentProviderOptions.SectionName);
        var options = section.Get<PaymentProviderOptions>() ?? new PaymentProviderOptions();
        var stripe = options.Stripe;

        if (!stripe.Enabled)
        {
            return services;
        }

        ValidateEnabled(stripe);

        if (environment.IsProduction()
            && (string.IsNullOrWhiteSpace(stripe.ApiKey) || string.IsNullOrWhiteSpace(stripe.WebhookSecret)))
        {
            throw new InvalidOperationException(
                "Payments:Providers:Stripe is enabled in Production but ApiKey/WebhookSecret are missing.");
        }

        StripeConfiguration.ApiKey = stripe.ApiKey;

        services.AddSingleton<IStripeGateway, StripeSdkGateway>();
        services.AddSingleton<StripeWebhookVerifier>();
        services.AddSingleton<StripeWebhookParser>();
        services.AddSingleton<IPaymentProvider, StripePaymentProvider>();
        return services;
    }

    private static void ValidateEnabled(StripeProviderOptions stripe)
    {
        // Enabled=true requires secrets in every environment (fail-fast at request time otherwise).
        if (string.IsNullOrWhiteSpace(stripe.ApiKey) || string.IsNullOrWhiteSpace(stripe.WebhookSecret))
        {
            throw new InvalidOperationException(
                "Payments:Providers:Stripe.Enabled is true but ApiKey or WebhookSecret is empty. " +
                "Disable Stripe or supply credentials before startup.");
        }

        if (string.IsNullOrWhiteSpace(stripe.SuccessUrl) || string.IsNullOrWhiteSpace(stripe.CancelUrl))
        {
            throw new InvalidOperationException(
                "Payments:Providers:Stripe.Enabled is true but SuccessUrl or CancelUrl is empty.");
        }
    }
}
