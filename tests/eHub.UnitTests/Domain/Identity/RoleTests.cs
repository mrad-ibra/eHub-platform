using eHub.Domain.Exceptions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Domain.Identity;

public sealed class RoleTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FromDefinition_SetsNormalizedNameAndSystemFlag()
    {
        var role = Role.FromDefinition(
            new RoleDefinition("Customer", "Default marketplace customer role.", IsSystem: true),
            Now);

        role.Name.Should().Be("Customer");
        role.NormalizedName.Should().Be("CUSTOMER");
        role.IsSystem.Should().BeTrue();
        role.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Rename_WhenSystemRole_Throws()
    {
        var role = Role.FromDefinition(new RoleDefinition("Admin", IsSystem: true), Now);

        var act = () => role.Rename("SuperAdmin", Now.AddMinutes(1));

        act.Should().Throw<ForbiddenAccessException>();
    }

    [Fact]
    public void Rename_WhenCustomRole_UpdatesName()
    {
        var role = Role.Create("Support", "Support agents", isSystem: false, Now);

        role.Rename("CustomerSupport", Now.AddMinutes(1));

        role.Name.Should().Be("CustomerSupport");
        role.NormalizedName.Should().Be("CUSTOMERSUPPORT");
        role.IsSystem.Should().BeFalse();
    }

    [Fact]
    public void AssignRole_AddsUserRoleOnce()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        var role = Role.FromDefinition(new RoleDefinition("Customer", IsSystem: true), Now);

        user.AssignRole(role, Now);
        user.AssignRole(role, Now.AddSeconds(1));

        user.Roles.Should().ContainSingle();
        user.HasRole(role.Id).Should().BeTrue();
    }

    [Fact]
    public void RemoveRole_RemovesAssignment()
    {
        var user = User.Register("user@ehub.local", "hash", "User", AccountKind.Personal, Now);
        var role = Role.FromDefinition(new RoleDefinition("Host", IsSystem: true), Now);
        user.AssignRole(role, Now);

        user.RemoveRole(role.Id, Now.AddMinutes(1));

        user.Roles.Should().BeEmpty();
        user.HasRole(role.Id).Should().BeFalse();
    }
}
