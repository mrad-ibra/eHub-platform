using eHub.Domain.Identity;
using eHub.Infrastructure.Persistence;

namespace eHub.UnitTests.Infrastructure.Persistence;

public sealed class InMemoryUserRepositoryTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetById_WhenSoftDeleted_ReturnsNull()
    {
        var repo = new InMemoryUserRepository();
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        await repo.AddAsync(user);
        user.SoftDelete(Now.AddMinutes(1));

        var found = await repo.GetByIdAsync(user.Id);

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmail_WhenSoftDeleted_ReturnsNull()
    {
        var repo = new InMemoryUserRepository();
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        await repo.AddAsync(user);
        user.SoftDelete(Now.AddMinutes(1));

        var found = await repo.GetByEmailAsync("user@ehub.local");

        found.Should().BeNull();
    }
}
