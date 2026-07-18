using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Identity;

namespace eHub.Application.Identity.Services;

public interface ILoginHistoryRecorder
{
    Task RecordSuccessAsync(
        Guid userId,
        string email,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task RecordFailureAsync(
        string email,
        LoginFailureReason failureReason,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
}

public sealed class LoginHistoryRecorder(
    ILoginHistoryRepository history,
    IClientContext clientContext,
    IClock clock,
    IUnitOfWork unitOfWork) : ILoginHistoryRecorder
{
    public async Task RecordSuccessAsync(
        Guid userId,
        string email,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var entry = LoginHistoryEntry.SucceededLogin(
            userId,
            email,
            sessionId,
            clock.UtcNow,
            clientContext.IpAddress,
            clientContext.UserAgent);

        await history.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailureAsync(
        string email,
        LoginFailureReason failureReason,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var entry = LoginHistoryEntry.FailedLogin(
            email,
            failureReason,
            clock.UtcNow,
            userId,
            clientContext.IpAddress,
            clientContext.UserAgent);

        await history.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
