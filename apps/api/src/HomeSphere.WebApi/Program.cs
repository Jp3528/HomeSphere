using HomeSphere.Infrastructure;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddProblemDetails();

// Infrastructure (PostgreSQL & EF Core)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Health Checks (Live and Database Ready)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HomeSphereDbContext>(
        name: "PostgreSQL",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "db" });

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HomeSphere API",
        Version = "v1",
        Description = "E-commerce avanzado para hogar + experiencia de ambientes interactivos"
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HomeSphere API v1");
    });

    // Auto-apply pending migrations in development
    await app.Services.ApplyMigrationsAsync();
}

app.UseCors("FrontendPolicy");

// Health check endpoints
app.MapHealthChecks("/health");

app.MapGet("/health/status", async (HealthCheckService healthCheckService) =>
{
    var report = await healthCheckService.CheckHealthAsync();
    var result = new
    {
        Status = report.Status.ToString(),
        Service = "HomeSphere.WebApi",
        Environment = app.Environment.EnvironmentName,
        TimestampUtc = DateTime.UtcNow,
        Checks = report.Entries.Select(e => new
        {
            Component = e.Key,
            Status = e.Value.Status.ToString(),
            Description = e.Value.Description,
            DurationMs = e.Value.Duration.TotalMilliseconds
        })
    };

    return report.Status == HealthStatus.Healthy ? Results.Ok(result) : Results.Json(result, statusCode: 503);
})
.WithName("GetHealthStatus")
.WithOpenApi();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Required for WebApplicationFactory in Integration Tests
public partial class Program { }
