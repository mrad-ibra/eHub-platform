namespace eHub.Application.Common.Context;

public interface IClientContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}
