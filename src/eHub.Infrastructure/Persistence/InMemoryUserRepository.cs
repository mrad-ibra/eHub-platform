using System.Collections.Concurrent;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Identity;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _usersById = new();
    private readonly ConcurrentDictionary<string, User> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_usersById.TryGetValue(userId, out var user) || user.IsDeleted)
        {
            return Task.FromResult<User?>(null);
        }

        return Task.FromResult<User?>(user);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!_usersByEmail.TryGetValue(email, out var user) || user.IsDeleted)
        {
            return Task.FromResult<User?>(null);
        }

        return Task.FromResult<User?>(user);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (!_usersById.TryAdd(user.Id, user) || !_usersByEmail.TryAdd(user.Email, user))
        {
            throw new InvalidOperationException($"User '{user.Email}' already exists.");
        }

        return Task.CompletedTask;
    }
}
