using eHub.Application.Abstractions.Audit;
using eHub.Application.Common.Time;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Application.Audit;

public sealed class AuditFieldStamperTests
{
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IAuditContext _audit = Substitute.For<IAuditContext>();
    private readonly DateTime _now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _actorId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public AuditFieldStamperTests()
    {
        _clock.UtcNow.Returns(_now);
        _audit.UserId.Returns(_actorId);
    }

    [Fact]
    public void StampCreated_SetsTimestampsAndActor()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, _now.AddHours(-1));
        var stamper = new AuditFieldStamper(_clock, _audit);

        stamper.StampCreated(user);

        user.CreatedAtUtc.Should().Be(_now);
        user.UpdatedAtUtc.Should().Be(_now);
        user.CreatedBy.Should().Be(_actorId);
        user.UpdatedBy.Should().Be(_actorId);
    }

    [Fact]
    public void StampUpdated_SetsTimestampAndActor()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, _now.AddHours(-1));
        var stamper = new AuditFieldStamper(_clock, _audit);

        stamper.StampUpdated(user);

        user.UpdatedAtUtc.Should().Be(_now);
        user.UpdatedBy.Should().Be(_actorId);
        user.CreatedAtUtc.Should().Be(_now.AddHours(-1));
    }

    [Fact]
    public void StampDeleted_SoftDeletesEntity()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, _now.AddHours(-1));
        var stamper = new AuditFieldStamper(_clock, _audit);

        stamper.StampDeleted(user);

        user.IsDeleted.Should().BeTrue();
        user.DeletedAtUtc.Should().Be(_now);
        user.DeletedBy.Should().Be(_actorId);
    }

    [Fact]
    public void StampRestored_RestoresEntity()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, _now.AddHours(-1));
        user.SoftDelete(_now.AddMinutes(-5), Guid.NewGuid());
        var stamper = new AuditFieldStamper(_clock, _audit);

        stamper.StampRestored(user);

        user.IsDeleted.Should().BeFalse();
        user.DeletedAtUtc.Should().BeNull();
        user.UpdatedBy.Should().Be(_actorId);
    }
}
