using System;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using NejPortalBackend.Infrastructure.Data;
using NejPortalBackend.Web.Hubs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration) // Read settings from appsettings.json
    .Enrich.FromLogContext() // Add contextual information
    .Enrich.WithEnvironmentName() // Include environment name
    .Enrich.WithMachineName() // Include machine name
    .Enrich.WithThreadId() // Include thread ID
    .WriteTo.Console() // Log to console
    .WriteTo.Debug() // Log to debug output (useful for local development)
    .WriteTo.File(
        path: $"logs/log-{DateTime.Now:yyyy-MM-dd}.txt",
        rollingInterval: RollingInterval.Day, // Create new log files per day
        retainedFileCountLimit: 7, // Keep logs for the last 7 days
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Replace default logging with Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddKeyVaultIfConfigured(builder.Configuration);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebServices();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  // await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseSerilogRequestLogging();
app.UseHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

// Apply CORS policy
app.UseCors("AllowSpecificOrigins");

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();
app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

// Custom error handler
app.UseExceptionHandler(options =>
{
    options.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var ex = context.Features.Get<IExceptionHandlerFeature>();
        if (ex != null)
        {
            var error = new { message = ex.Error.Message };
            await context.Response.WriteAsJsonAsync(error);
        }
    });
});

app.Map("/", () => Results.Redirect("/api"));

app.MapEndpoints();

app.Run();

public partial class Program { }
