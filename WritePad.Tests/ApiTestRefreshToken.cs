
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

    // ==========================================
    // TEST 1: REFRESH TOKEN ROTATION
    // ==========================================
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
            Username = $"refreshuser_{Guid.NewGuid():N}",
            Email = $"refreshuser_{Guid.NewGuid():N}@example.com",
            PasswordHash = passwordHash,
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Username = user.Username,
                Password = "Test@12345"
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginJson = await loginResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var oldRefreshToken = loginJson
            .GetProperty("refreshToken").GetString();

        Assert.False(string.IsNullOrEmpty(oldRefreshToken));

        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/Auth/refresh",
            new
            {
                RefreshToken = oldRefreshToken
            });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshJson = await refreshResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var newAccessToken = refreshJson
            .GetProperty("accessToken").GetString();

        var newRefreshToken = refreshJson
            .GetProperty("refreshToken").GetString();

        Assert.False(string.IsNullOrEmpty(newAccessToken));
        Assert.False(string.IsNullOrEmpty(newRefreshToken));
        Assert.NotEqual(oldRefreshToken, newRefreshToken);

        var oldTokenEntity = await context.RefreshTokems
            .FirstOrDefaultAsync(x => x.Token == oldRefreshToken);

        Assert.NotNull(oldTokenEntity);
        Assert.True(oldTokenEntity.IsRevoked);

        var newTokenEntity = await context.RefreshTokems
            .FirstOrDefaultAsync(x => x.Token == newRefreshToken);

        Assert.NotNull(newTokenEntity);
        Assert.False(newTokenEntity.IsRevoked);
    }

    // ==========================================
    // TEST 2: REUSE REVOKED REFRESH TOKEN
    // ==========================================
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
            .GetProperty("refreshToken").GetString();

        Assert.False(string.IsNullOrEmpty(oldRefreshToken));

        var firstRefreshResponse = await _client.PostAsJsonAsync(
            "/api/Auth/refresh",
            new
            {
                RefreshToken = oldRefreshToken
            });

        Assert.Equal(HttpStatusCode.OK, firstRefreshResponse.StatusCode);

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

    // ==========================================
    // TEST 3: EXPIRED REFRESH TOKEN
    // ==========================================
    [Fact]
    public async Task Expired_Refresh_Token_Should_Return_Unauthorized()
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<TokenDbContext>();

        var passwordService = scope.ServiceProvider
            .GetRequiredService<PasswordService>();

        var username = $"expireduser_{Guid.NewGuid():N}";

        var user = new User
        {
            Username = username,
            Email = $"{username}@example.com",
            PasswordHash = passwordService.HashPassword("Test@12345"),
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var expiredToken = new RefreshTokem
        {
            Username = username,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false
        };

        context.RefreshTokems.Add(expiredToken);
        await context.SaveChangesAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/Auth/refresh",
            new
            {
                RefreshToken = expiredToken.Token
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // ==========================================
    // TEST 4: LOGOUT SHOULD REVOKE TOKEN
    // ==========================================
    [Fact]
    public async Task Logout_Should_Revoke_Refresh_Token()
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<TokenDbContext>();

        var passwordService = scope.ServiceProvider
            .GetRequiredService<PasswordService>();

        var username = $"logoutuser_{Guid.NewGuid():N}";

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

        var refreshToken = loginJson
            .GetProperty("refreshToken").GetString();

        Assert.False(string.IsNullOrEmpty(refreshToken));

        // Logout
        var logoutResponse = await _client.PostAsJsonAsync(
            "/api/Auth/logout",
            new
            {
                RefreshToken = refreshToken
            });

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Verify token is revoked
        var tokenEntity = await context.RefreshTokems
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        Assert.NotNull(tokenEntity);
        Assert.True(tokenEntity.IsRevoked);
    }

    // ==========================================
    // TEST 5: LOGGED OUT TOKEN CANNOT BE REUSED
    // ==========================================
    [Fact]
    public async Task Logged_Out_Refresh_Token_Should_Return_Unauthorized()
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<TokenDbContext>();

        var passwordService = scope.ServiceProvider
            .GetRequiredService<PasswordService>();

        var username = $"logoutuser_{Guid.NewGuid():N}";

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

        var refreshToken = loginJson
            .GetProperty("refreshToken").GetString();

        Assert.False(string.IsNullOrEmpty(refreshToken));

        // Logout
        var logoutResponse = await _client.PostAsJsonAsync(
            "/api/Auth/logout",
            new
            {
                RefreshToken = refreshToken
            });

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Attempt refresh using logged-out token
        var refreshResponse = await _client.PostAsJsonAsync(
            "/api/Auth/refresh",
            new
            {
                RefreshToken = refreshToken
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);
    }
}


