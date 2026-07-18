using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Commands.ConfirmEmail;
using eHub.Application.Identity.Commands.Login;
using eHub.Domain.Exceptions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Application.Identity;

public sealed class ConfirmEmailCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAuthSessionFactory _sessionFactory = Substitute.For<IAuthSessionFactory>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    public ConfirmEmailCommandHandlerTests()
    {
        _clock.UtcNow.Returns(_now);
    }

    [Fact]
    public async Task Handle_WithValidToken_ConfirmsAndReturnsSession()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, _now);
        user.SetEmailVerificationToken("valid-token", _now.AddHours(24), _now);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _sessionFactory.CreateAsync(user, Arg.Any<CancellationToken>())
            .Returns(new AuthSessionResult(
                user.Id,
                user.Email,
                user.FullName,
                "Personal",
                ["Customer"],
                "access-token",
                _now.AddHours(1),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                "refresh-token",
                _now.AddDays(14)));

        var handler = new ConfirmEmailCommandHandler(_users, _sessionFactory, _unitOfWork, _clock);
        var result = await handler.Handle(new ConfirmEmailCommand(user.Id, "valid-token"), CancellationToken.None);

        user.IsEmailConfirmed.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidToken_Throws()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, _now);
        user.SetEmailVerificationToken("valid-token", _now.AddHours(24), _now);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var handler = new ConfirmEmailCommandHandler(_users, _sessionFactory, _unitOfWork, _clock);
        var act = () => handler.Handle(new ConfirmEmailCommand(user.Id, "wrong-token"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationFailedException>();
    }

    [Fact]
    public async Task Handle_WhenUserMissing_Throws()
    {
        _users.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new ConfirmEmailCommandHandler(_users, _sessionFactory, _unitOfWork, _clock);
        var act = () => handler.Handle(new ConfirmEmailCommand(Guid.NewGuid(), "token"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
