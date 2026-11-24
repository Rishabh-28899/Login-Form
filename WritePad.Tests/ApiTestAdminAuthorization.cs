using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using WritePad.Tests.Infrastructure;
using Xunit;

namespace WritePad.Tests;

public class ApiTestAdminAuthorization
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TokenValidationParameters _validationParameters;

    public ApiTestAdminAuthorization(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        var jwtOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        _validationParameters = jwtOptions.TokenValidationParameters;
    }

    private string GenerateJwtToken(string role)
    {
        var key = _validationParameters.IssuerSigningKey
            ?? throw new InvalidOperationException("JWT signing key not found.");

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1002"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _validationParameters.ValidIssuer,
            audience: _validationParameters.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Regular_User_Cannot_Access_Admin_Users_Endpoint()
    {
        var token = GenerateJwtToken("User");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Admin/users");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task Admin_User_Can_Access_Admin_Users_Endpoint()
    {
        var token = GenerateJwtToken("Admin");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Admin/users");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task Admin_Can_Access_Admin_Dashboard()
    {
        var token = GenerateJwtToken("Admin");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Admin/dashboard");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task Regular_User_Cannot_Access_Admin_Dashboard()
    {
        var token = GenerateJwtToken("User");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Admin/dashboard");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task Admin_Can_Access_All_Notes_Endpoint()
    {
        var token = GenerateJwtToken("Admin");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Admin/notes");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task Admin_Can_Create_New_User()
    {
        var token = GenerateJwtToken("Admin");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/Admin/users");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(new
        {
            Username = "testuser123",
            Email = "testuser123@example.com",
            Password = "Test@12345",
            Role = "User"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task Regular_User_Cannot_Create_New_User()
    {
        var token = GenerateJwtToken("User");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/Admin/users");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(new
        {
            Username = "testuser456",
            Email = "testuser456@example.com",
            Password = "Test@12345",
            Role = "User"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}


