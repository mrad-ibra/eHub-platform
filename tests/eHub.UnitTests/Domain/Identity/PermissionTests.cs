using eHub.Domain.Exceptions;
using eHub.Domain.Identity;

namespace eHub.UnitTests.Domain.Identity;

public sealed class PermissionTests
{
    private static readonly DateTime Now = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FromDefinition_NormalizesCode()
    {
        var definition = new PermissionDefinition(
            "Catalog.Assets.Create",
            "Create assets",
            "Catalog",
            "Allows creating asset listings.");

        var permission = Permission.FromDefinition(definition, isSystem: true, Now);

        permission.Code.Should().Be("catalog.assets.create");
        permission.Module.Should().Be("Catalog");
        permission.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void PermissionCode_WhenMissingDot_Throws()
    {
        var act = () => PermissionCode.Create("assetscreate");

        act.Should().Throw<ValidationFailedException>();
    }

    [Fact]
    public void Rename_WhenSystemPermission_Throws()
    {
        var permission = Permission.FromDefinition(
            new PermissionDefinition("identity.users.read", "Read users", "Identity"),
            isSystem: true,
            Now);

        var act = () => permission.Rename("View users", Now.AddMinutes(1));

        act.Should().Throw<ForbiddenAccessException>();
    }

    [Fact]
    public void GrantPermission_AddsOnce()
    {
        var role = Role.FromDefinition(new RoleDefinition("Host", IsSystem: true), Now);
        var permission = Permission.FromDefinition(
            new PermissionDefinition("catalog.assets.manage", "Manage assets", "Catalog"),
            isSystem: true,
            Now);

        role.GrantPermission(permission, Now);
        role.GrantPermission(permission, Now.AddSeconds(1));

        role.Permissions.Should().ContainSingle();
        role.HasPermission(permission.Id).Should().BeTrue();
    }

    [Fact]
    public void RevokePermission_RemovesGrant()
    {
        var role = Role.FromDefinition(new RoleDefinition("Admin", IsSystem: true), Now);
        var permission = Permission.FromDefinition(
            new PermissionDefinition("identity.roles.manage", "Manage roles", "Identity"),
            isSystem: true,
            Now);
        role.GrantPermission(permission, Now);

        role.RevokePermission(permission.Id, Now.AddMinutes(1));

        role.Permissions.Should().BeEmpty();
        role.HasPermission(permission.Id).Should().BeFalse();
    }
}
