using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Infrastructure.Payments;
using eHub.Infrastructure.Payments.Providers.Stripe;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace eHub.UnitTests.Infrastructure.Payments;

public sealed class PaymentProviderResolverTests
{
    private readonly PaymentProviderResolver _resolver;

    public PaymentProviderResolverTests()
    {
        var options = Options.Create(new PaymentProviderOptions
        {
            Stripe = new StripeProviderOptions
            {
                Enabled = true,
                ApiKey = "sk_test",
                WebhookSecret = "whsec_test",
                SuccessUrl = "https://ok.example/{PAYMENT_ID}",
                CancelUrl = "https://cancel.example/{PAYMENT_ID}"
            }
        });

        var gateway = Substitute.For<IStripeGateway>();
        var verifier = new StripeWebhookVerifier(gateway, options);
        var parser = new StripeWebhookParser(new MinorUnitConverter());
        var stripe = new StripePaymentProvider(
            gateway,
            verifier,
            parser,
            new MinorUnitConverter(),
            options);

        _resolver = new PaymentProviderResolver([
            new FakePaymentProvider(Options.Create(new PaymentProviderOptions())),
            stripe,
            new PayriffPaymentProvider()
        ]);
    }

    [Theory]
    [InlineData(PaymentProviderCodes.Test, typeof(FakePaymentProvider))]
    [InlineData(PaymentProviderCodes.Stripe, typeof(StripePaymentProvider))]
    [InlineData(PaymentProviderCodes.Payriff, typeof(PayriffPaymentProvider))]
    public void GetRequired_ResolvesRegisteredProviders(string key, Type expectedType)
    {
        var provider = _resolver.GetRequired(key);
        provider.ProviderKey.Should().Be(key);
        provider.Should().BeOfType(expectedType);
    }

    [Fact]
    public void GetRequired_UnknownProvider_ThrowsNotFound()
    {
        var act = () => _resolver.GetRequired("UNKNOWN");
        act.Should().Throw<eHub.Domain.Exceptions.NotFoundException>();
    }
}
