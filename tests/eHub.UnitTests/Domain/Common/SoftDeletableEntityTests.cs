using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Domain.Common;

public sealed class SoftDeletableEntityTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ActorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void SoftDelete_MarksEntityDeleted()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);

        user.SoftDelete(Now.AddMinutes(1), ActorId);

        user.IsDeleted.Should().BeTrue();
        user.DeletedAtUtc.Should().Be(Now.AddMinutes(1));
        user.DeletedBy.Should().Be(ActorId);
        user.UpdatedBy.Should().Be(ActorId);
    }

    [Fact]
    public void Restore_ClearsSoftDelete()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        user.SoftDelete(Now.AddMinutes(1), ActorId);

        user.Restore(Now.AddMinutes(2), ActorId);

        user.IsDeleted.Should().BeFalse();
        user.DeletedAtUtc.Should().BeNull();
        user.DeletedBy.Should().BeNull();
        user.UpdatedAtUtc.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void UpdateProfile_WhenDeleted_Throws()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        user.SoftDelete(Now.AddMinutes(1));

        var act = () => user.UpdateProfile("New", null, Now.AddMinutes(2));

        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void WhereNotDeleted_FiltersDeletedItems()
    {
        var active = User.Register("a@ehub.local", "hash", "A", AccountKind.Personal, Now);
        var deleted = User.Register("b@ehub.local", "hash", "B", AccountKind.Personal, Now);
        deleted.SoftDelete(Now.AddMinutes(1));

        var result = new[] { active, deleted }.WhereNotDeleted().ToArray();

        result.Should().ContainSingle().Which.Email.Should().Be("a@ehub.local");
    }
}
