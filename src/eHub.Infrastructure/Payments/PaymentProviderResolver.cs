using System.Collections.Concurrent;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Infrastructure.Payments;

public sealed class PaymentProviderResolver(IEnumerable<IPaymentProvider> providers) : IPaymentProviderResolver
{
    private readonly ConcurrentDictionary<string, IPaymentProvider> _map = new(
        providers.ToDictionary(p => p.ProviderKey, StringComparer.OrdinalIgnoreCase),
        StringComparer.OrdinalIgnoreCase);

    public IPaymentProvider GetRequired(string providerKey)
    {
        if (_map.TryGetValue(providerKey.Trim(), out var provider))
        {
            return provider;
        }

        throw new NotFoundException(ErrorResources.Get(ErrorCodes.PaymentProviderCodeInvalid));
    }
}
