using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace HomeSphere.IntegrationTests.Auth;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterAndLoginFlow_ShouldAuthenticateAndAccessProtectedProfile()
    {
        var email = $"integration_{Guid.NewGuid():N}@homesphere.com";
        var password = "Password123!";

        // 1. Register
        var registerPayload = new
        {
            email,
            password,
            firstName = "María",
            lastName = "García"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerPayload);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // 2. Login
        var loginPayload = new
        {
            email,
            password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginJson.GetProperty("accessToken").GetString();
        Assert.NotNull(token);

        // 3. Access Protected /api/me
        using var profileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var profileResponse = await _client.SendAsync(profileRequest);
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profileJson = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, profileJson.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
