using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using TranscriptAnalyzer.POC.Infrastructure.Logging.Enrichers;

namespace TranscriptAnalyzer.POC.Infrastructure.Logging
{
    public static class SerilogConfiguration
    {
        public static IServiceCollection AddStartupSerilogLogging(this IServiceCollection services)
        {
            // Serilog bootstrap logger (for startup errors)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .WriteTo.Console(formatter: new Serilog.Formatting.Json.JsonFormatter())
                .CreateLogger();

            return services;
        }


        public static void UseSerilog(this ConfigureHostBuilder host)
        {
            host.UseSerilog((ctx, cfg) =>
            {
                cfg
                   .MinimumLevel.Warning()
                   .MinimumLevel.Override("TranscriptAnalyzer.POC.Web", LogEventLevel.Information)
                   .MinimumLevel.Override("TranscriptAnalyzer.POC.Infrastructure", LogEventLevel.Information)
                   .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                   .Enrich.FromLogContext()
                   .Enrich.WithMachineName()
                   .Enrich.WithProcessId()
                   .Enrich.WithThreadId()
                   .Enrich.WithEnvironmentVariable(environmentVariableName: "ASPNETCORE_ENVIRONMENT", propertyName: "ASPNETCORE_ENVIRONMENT")
                   .Enrich.With(new RedactEnricher(["password", "token", "authorization", "ssn"]))
                   .Enrich.WithProperty("Application", ctx.HostingEnvironment.ApplicationName)
                   .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
                   .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
                   .WriteTo.ApplicationInsights(
                       ctx.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"],
                       TelemetryConverter.Traces
                   );
                // Optional sinks:
                // .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341")
                // .WriteTo.File("logs/log-.ndjson", rollingInterval: RollingInterval.Day);
            });

        }



    }
}
