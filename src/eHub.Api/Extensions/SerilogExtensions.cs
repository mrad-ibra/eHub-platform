using Serilog;

namespace eHub.Api.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddEHubSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

        return builder;
    }

    public static WebApplication UseEHubSerilog(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        return app;
    }
}
