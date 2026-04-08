using Microsoft.AspNetCore.Builder;
using Serilog;
using TranscriptAnalyzer.POC.Infrastructure.Application.Middlewares;

namespace TranscriptAnalyzer.POC.Infrastructure.Application
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseAppMiddleWares(this IApplicationBuilder app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();

            return app;
        }


        public static IApplicationBuilder UseSerilogEnrichers(this IApplicationBuilder app)
        {
            app.UseSerilogRequestLogging(opts =>
            {
                // Each request: { requestPath, statusCode, elapsedMs, correlationId, user }
                opts.EnrichDiagnosticContext = (diag, http) =>
                {
                    diag.Set("CorrelationId", http.TraceIdentifier);
                    diag.Set("UserId", http.User?.Identity?.IsAuthenticated == true
                        ? http.User.Identity!.Name
                        : null);
                    diag.Set("ClientIP", http.Connection.RemoteIpAddress?.ToString());
                };
            });


            return app;
        }

    }
}
