using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

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
}

app.UseCors("FrontendPolicy");

// Health check endpoints
app.MapHealthChecks("/health");

app.MapGet("/health/status", () => Results.Ok(new
{
    Status = "Healthy",
    Service = "HomeSphere.WebApi",
    Environment = app.Environment.EnvironmentName,
    TimestampUtc = DateTime.UtcNow
}))
.WithName("GetHealthStatus")
.WithOpenApi();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Required for WebApplicationFactory in Integration Tests
public partial class Program { }
