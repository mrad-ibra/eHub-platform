using eHub.Domain.Identity;

namespace eHub.UnitTests.Domain.Common;

public sealed class AuditableEntityTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ActorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Register_SetsCreatedAuditWithoutActor()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);

        user.CreatedAtUtc.Should().Be(Now);
        user.UpdatedAtUtc.Should().Be(Now);
        user.CreatedBy.Should().BeNull();
        user.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_WithActor_SetsUpdatedBy()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        var updatedAt = Now.AddMinutes(5);

        user.UpdateProfile("New Name", null, updatedAt, ActorId);

        user.UpdatedAtUtc.Should().Be(updatedAt);
        user.UpdatedBy.Should().Be(ActorId);
        user.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void SetCreatedAudit_OverwritesActorAndTimestamps()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);

        user.SetCreatedAudit(Now.AddHours(1), ActorId);

        user.CreatedAtUtc.Should().Be(Now.AddHours(1));
        user.UpdatedAtUtc.Should().Be(Now.AddHours(1));
        user.CreatedBy.Should().Be(ActorId);
        user.UpdatedBy.Should().Be(ActorId);
    }
}
