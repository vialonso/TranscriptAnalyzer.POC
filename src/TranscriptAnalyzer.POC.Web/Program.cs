using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using TranscriptAnalyzer.POC.Infrastructure.Application;
using TranscriptAnalyzer.POC.Infrastructure.Configuration;
using TranscriptAnalyzer.POC.Infrastructure.DependencyInjection;
using TranscriptAnalyzer.POC.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStartupSerilogLogging();
builder.Configuration.AddConfigurationJsonFiles();
builder.Host.UseSerilog();

builder.Services.AddRazorPages();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddApplicationInsightsTelemetry(new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
{
    ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseSerilogEnrichers();
app.UseAppMiddleWares();

app.UseExceptionHandler(appErr =>
{
    appErr.Run(async ctx =>
    {
        var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        Log.ForContext("Path", ctx.Request.Path)
           .ForContext("CorrelationId", ctx.TraceIdentifier)
           .Error(ex, "Unhandled exception");
        ctx.Response.StatusCode = 500;
        await ctx.Response.WriteAsJsonAsync(new
        {
            type = "about:blank",
            title = "An error occurred.",
            status = 500,
            traceId = ctx.TraceIdentifier
        });
    });
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
