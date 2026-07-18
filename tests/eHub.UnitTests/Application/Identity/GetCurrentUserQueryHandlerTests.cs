using eHub.Application.Common.Context;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Queries.GetCurrentUser;
using eHub.Domain.Exceptions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Application.Identity;

public sealed class GetCurrentUserQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ReturnsProfileForAuthenticatedUser()
    {
        var user = User.SeedAdmin("admin@ehub.local", "hash", "Admin", _now);
        _currentUser.RequireUserId().Returns(user.Id);
        _currentUser.Roles.Returns(["Admin"]);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new GetCurrentUserQueryHandler(_currentUser, _users);
        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.Id.Should().Be(user.Id);
        result.Email.Should().Be("admin@ehub.local");
        result.FullName.Should().Be("Admin");
        result.AccountKind.Should().Be("Admin");
        result.IsEmailConfirmed.Should().BeTrue();
        result.Roles.Should().Equal("Admin");
    }

    [Fact]
    public async Task Handle_WhenUserMissing_Throws()
    {
        var userId = Guid.NewGuid();
        _currentUser.RequireUserId().Returns(userId);
        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new GetCurrentUserQueryHandler(_currentUser, _users);
        var act = () => handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
