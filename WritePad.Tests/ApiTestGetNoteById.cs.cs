
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using WritePad.Tests.Infrastructure;

namespace WritePad.Tests;

public class ApiTestGetNoteById
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TokenValidationParameters _validationParameters;

    public ApiTestGetNoteById(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        var jwtOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        _validationParameters = jwtOptions.TokenValidationParameters;
    }

    private string GenerateJwtToken()
    {
        var key = _validationParameters.IssuerSigningKey
            ?? throw new InvalidOperationException("JWT signing key not found.");

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1002"),
            new Claim(ClaimTypes.Name, "testuser")
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
    public async Task Get_Note_By_Id_With_Valid_Token_Should_Return_Ok()
    {
        // Arrange
        var token = GenerateJwtToken();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/Notes",
            new
            {
                Title = "Integration Test Note",
                Content = "Testing GET note by ID",
                Topic = "Testing"
            });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createdNote = await createResponse.Content.ReadFromJsonAsync<CreateNoteResponse>();

        Assert.NotNull(createdNote);

        // Act
        var response = await _client.GetAsync(
            $"/api/Notes/{createdNote.noteId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private class CreateNoteResponse
    {
        public int noteId { get; set; }
    }
}

