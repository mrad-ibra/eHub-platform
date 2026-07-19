using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace eHub.Persistence;

public sealed class EHubDbContextFactory : IDesignTimeDbContextFactory<EHubDbContext>
{
    public EHubDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "eHub.Api"));
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=ehub;Username=ehub;Password=ehub";

        var options = new DbContextOptionsBuilder<EHubDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EHubDbContext(options);
    }
}
