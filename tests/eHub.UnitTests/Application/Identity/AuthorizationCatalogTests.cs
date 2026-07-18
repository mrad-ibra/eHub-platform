using eHub.Application.Configuration;
using eHub.Application.Identity.Authorization;
using eHub.Domain.Identity;
using Microsoft.Extensions.Options;

namespace eHub.UnitTests.Application.Identity;

public sealed class AuthorizationCatalogTests
{
    [Fact]
    public void Catalog_MergesConfigurationAndValidatesRolePermissions()
    {
        var options = Options.Create(new AuthorizationOptions
        {
            Permissions =
            [
                new PermissionDefinition("catalog.assets.read", "Read assets", "Catalog"),
                new PermissionDefinition("bookings.create", "Create bookings", "Bookings")
            ],
            Roles =
            [
                new RoleDefinition(
                    "Customer",
                    "Customer role",
                    ["catalog.assets.read", "bookings.create"],
                    IsSystem: true)
            ]
        });

        var catalog = new AuthorizationCatalog(options, [], []);

        catalog.Permissions.Should().HaveCount(2);
        catalog.FindRole("Customer")!.Permissions.Should().BeEquivalentTo(
            "catalog.assets.read",
            "bookings.create");
        catalog.FindPermission("CATALOG.ASSETS.READ")!.Name.Should().Be("Read assets");
    }

    [Fact]
    public void Catalog_WhenRoleReferencesUnknownPermission_Throws()
    {
        var options = Options.Create(new AuthorizationOptions
        {
            Permissions =
            [
                new PermissionDefinition("catalog.assets.read", "Read assets", "Catalog")
            ],
            Roles =
            [
                new RoleDefinition("Broken", Permissions: ["catalog.assets.delete"])
            ]
        });

        var act = () => _ = new AuthorizationCatalog(options, [], []);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*catalog.assets.delete*");
    }

    [Fact]
    public void Catalog_MergesModuleProviders()
    {
        var options = Options.Create(new AuthorizationOptions());
        var provider = new FakePermissionProvider();

        var catalog = new AuthorizationCatalog(options, [provider], []);

        catalog.FindPermission("payments.refunds.manage").Should().NotBeNull();
    }

    private sealed class FakePermissionProvider : IPermissionDefinitionProvider
    {
        public string Module => "Payments";

        public IReadOnlyCollection<PermissionDefinition> GetDefinitions() =>
        [
            new PermissionDefinition("payments.refunds.manage", "Manage refunds", Module)
        ];
    }
}
