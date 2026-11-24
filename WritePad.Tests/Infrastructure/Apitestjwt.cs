
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Xunit;
using WritePad.Tests.Infrastructure;

namespace WritePad.Tests;

public class ApiTestJwt
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TokenValidationParameters _validationParameters;

    public ApiTestJwt(CustomWebApplicationFactory factory)
    {
        // Create HTTP client
        _client = factory.CreateClient();

        // Get actual JWT validation settings from API
        var jwtOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        _validationParameters = jwtOptions.TokenValidationParameters;
    }

    // Generate JWT Token for Testing
    private string GenerateJwtToken()
    {
        // Get the same signing key used by API
        var key = _validationParameters.IssuerSigningKey
            ?? throw new InvalidOperationException(
                "JWT signing key not found."
            );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        // Test user claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1002"),
            new Claim(ClaimTypes.Name, "testuser")
        };

        // Generate JWT
        var token = new JwtSecurityToken(
            issuer: _validationParameters.ValidIssuer,
            audience: _validationParameters.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Test: Valid JWT Token
    // Expected: 200 OK
    [Fact]
    public async Task Get_Notes_With_Valid_Token_Should_Return_Ok()
    {
        // Arrange
        var token = GenerateJwtToken();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Notes"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );
    }
}
