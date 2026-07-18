using eHub.Application.Common.Context;

namespace eHub.Api.Common;

public sealed class HttpClientContext(IHttpContextAccessor httpContextAccessor) : IClientContext
{
    public string? IpAddress
        => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var headers = httpContextAccessor.HttpContext?.Request.Headers;
            if (headers is null || !headers.TryGetValue("User-Agent", out var values))
            {
                return null;
            }

            var value = values.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
