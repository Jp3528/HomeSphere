using System.Text;
using HomeSphere.Application;
using HomeSphere.Infrastructure;
using HomeSphere.Infrastructure.Persistence;
using HomeSphere.WebApi.BackgroundServices;
using HomeSphere.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers & Global ProblemDetails
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Application & Infrastructure Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Background Workers
builder.Services.AddHostedService<ExpiredReservationWorker>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "development_super_secret_jwt_key_at_least_32_chars_long!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HomeSphereApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HomeSphereWeb";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

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

// OpenAPI / Swagger with JWT Security Definition
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HomeSphere API",
        Version = "v1",
        Description = "E-commerce avanzado para hogar + experiencia de ambientes interactivos"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Format: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Correlation ID & Exception Handler Pipeline
app.UseMiddleware<CorrelationIdMiddleware>();
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

    // Auto-apply pending migrations and execute seed in development
    await app.Services.ApplyMigrationsAsync();
}

app.UseCors("FrontendPolicy");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

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

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Required for WebApplicationFactory in Integration Tests
public partial class Program { }
