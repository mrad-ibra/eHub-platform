using eHub.Api.Extensions;
using eHub.Api.Filters;
using eHub.Application;
using eHub.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting eHub API");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddEHubSerilog();
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddEHubProblemDetails();
    builder.Services.AddEHubHealthChecks(builder.Configuration);
    builder.Services.AddEHubVersioning();
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<FluentValidationActionFilter>();
    });
    builder.Services.AddEHubSwagger();
    builder.Services.AddEHubAuth(builder.Configuration);

    var app = builder.Build();

    app.UseGlobalExceptionMiddleware();
    app.UseEHubSerilog();

    if (app.Environment.IsDevelopment())
    {
        app.UseEHubSwagger();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapEHubHealthChecks();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "eHub API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
