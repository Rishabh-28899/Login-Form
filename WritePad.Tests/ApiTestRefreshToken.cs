
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Token2Core.Data;
using WritePad.Model;
using WritePad.Services;

using WritePad.Tests.Infrastructure;
using Xunit;

namespace WritePad.Tests;

public class ApiTestRefreshToken
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ApiTestRefreshToken(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_Token_Should_Return_New_Tokens()
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<TokenDbContext>();

        var passwordService = scope.ServiceProvider
            .GetRequiredService<PasswordService>();

        var passwordHash = passwordService.HashPassword("Test@12345");

        var user = new User
        {
            Username = "refreshuser",
            Email = "refreshuser@example.com",
            PasswordHash = passwordHash,
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        // Login using the test user
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Username = "refreshuser",
                Password = "Test@12345"
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Read login response
        var loginJson = await loginResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var oldRefreshToken = loginJson
            .GetProperty("refreshToken")
            .GetString();

        Assert.False(string.IsNullOrEmpty(oldRefreshToken));
        // Send refresh token to refresh endpoint
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/Auth/refresh",
            new
            {
                RefreshToken = oldRefreshToken
            });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        // Read new tokens
        var refreshJson = await refreshResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var newAccessToken = refreshJson
            .GetProperty("accessToken")
            .GetString();

        var newRefreshToken = refreshJson
            .GetProperty("refreshToken")
            .GetString();

        Assert.False(string.IsNullOrEmpty(newAccessToken));
        Assert.False(string.IsNullOrEmpty(newRefreshToken));

        Assert.NotEqual(oldRefreshToken, newRefreshToken);
        // Verify old refresh token is revoked
        var oldTokenEntity = await context.RefreshTokems
            .FirstOrDefaultAsync(x => x.Token == oldRefreshToken);

        Assert.NotNull(oldTokenEntity);
        Assert.True(oldTokenEntity.IsRevoked);

        // Verify new refresh token is stored
        var newTokenEntity = await context.RefreshTokems
            .FirstOrDefaultAsync(x => x.Token == newRefreshToken);

        Assert.NotNull(newTokenEntity);
        Assert.False(newTokenEntity.IsRevoked);
    }
    [Fact]
    public async Task Reusing_Revoked_Refresh_Token_Should_Return_Unauthorized()
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<TokenDbContext>();

        var passwordService = scope.ServiceProvider
            .GetRequiredService<PasswordService>();

        var username = $"refreshuser_{Guid.NewGuid():N}";

        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = passwordService.HashPassword("Test@12345"),
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Login
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Username = username,
                Password = "Test@12345"
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginJson = await loginResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var oldRefreshToken = loginJson
            .GetProperty("refreshToken")
            .GetString();

        Assert.False(string.IsNullOrEmpty(oldRefreshToken));
        // First refresh: should succeed
        var firstRefreshResponse = await _client.PostAsJsonAsync(
            "/api/Auth/refresh",
            new
            {
                RefreshToken = oldRefreshToken
            });

        Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);
        // Reuse the same old refresh token: should fail
        var reuseResponse = await _client.PostAsJsonAsync(
            "/api/Auth/refresh",
            new
            {
                RefreshToken = oldRefreshToken
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            reuseResponse.StatusCode);
    }

} 

