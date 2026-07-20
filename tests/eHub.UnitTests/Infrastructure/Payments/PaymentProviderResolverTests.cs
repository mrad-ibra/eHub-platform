using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Infrastructure.Payments;

namespace eHub.UnitTests.Infrastructure.Payments;

public sealed class PaymentProviderResolverTests
{
    private readonly PaymentProviderResolver _resolver = new([
        new FakePaymentProvider(Microsoft.Extensions.Options.Options.Create(
            new eHub.Application.Configuration.PaymentProviderOptions())),
        new StripePaymentProvider(),
        new PayriffPaymentProvider()
    ]);

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

public sealed class PaymentProviderSkeletonTests
{
    [Fact]
    public async Task StripeSkeleton_CreatePayment_ThrowsNotWired()
    {
        var provider = new StripePaymentProvider();
        var act = () => provider.CreatePaymentAsync(
            new ProviderCreatePaymentRequest(Guid.NewGuid(), Guid.NewGuid(), 10m, Guid.NewGuid(), "key"),
            CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*STRIPE*not yet wired*");
    }

    [Fact]
    public void StripeSkeleton_VerifyWebhook_ReturnsFalse()
    {
        var provider = new StripePaymentProvider();
        provider.VerifyWebhook(new Dictionary<string, string>(), [] , DateTime.UtcNow).Should().BeFalse();
    }
}
