using System.Net;
using System.Text.Json;
using Xunit;

namespace HomeSphere.IntegrationTests.Health;

public class HealthCheckIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthStatus_ShouldReturnDetailedServiceReport()
    {
        var response = await _client.GetAsync("/health/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);

        var root = jsonDoc.RootElement;
        Assert.True(root.TryGetProperty("service", out var serviceProp) || root.TryGetProperty("Service", out serviceProp));
        Assert.Equal("HomeSphere.WebApi", serviceProp.GetString());
    }
}
