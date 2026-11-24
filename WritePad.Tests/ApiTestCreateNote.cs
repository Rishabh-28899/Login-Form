
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Token2Core.Data;
using WritePad.Tests.Infrastructure;
using Xunit;

namespace WritePad.Tests;

public class ApiTestCreateNote
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly TokenValidationParameters _validationParameters;

    public ApiTestCreateNote(CustomWebApplicationFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient();

        var jwtOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        _validationParameters = jwtOptions.TokenValidationParameters;
    }

    // Generate JWT Token for Testing
    private string GenerateJwtToken()
    {
        var key = _validationParameters.IssuerSigningKey
            ?? throw new InvalidOperationException(
                "JWT signing key not found.");

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

    // Test: Create Note with Valid JWT
    [Fact]
    public async Task Post_Notes_With_Valid_Token_Should_Save_Note_For_Authenticated_User()
    {
        // Arrange
        var token = GenerateJwtToken();

        var note = new
        {
            Title = "Integration Test Note",
            Content = "Testing note creation using JWT authentication",
            Topic = "Testing"
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/Notes");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(note);

        // Act
        var response = await _client.SendAsync(request);

        // Assert 1: API should return 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Read response body
        var responseBody = await response.Content.ReadAsStringAsync();

        using var jsonDocument = JsonDocument.Parse(responseBody);

        var root = jsonDocument.RootElement;

        int noteId = root.GetProperty("noteId").GetInt32();
        int userId = root.GetProperty("userId").GetInt32();

        // Assert 2: API response should contain authenticated UserId
        Assert.Equal(1002, userId);

        // Assert 3: Verify note actually exists in database
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<TokenDbContext>();

        var savedNote = await context.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == noteId);

        Assert.NotNull(savedNote);

        // Assert 4: Verify note belongs to UserId 1002
        Assert.Equal(1002, savedNote.UserId);

        // Assert 5: Verify saved note details
        Assert.Equal(note.Title, savedNote.Title);
        Assert.Equal(note.Content, savedNote.Content);
    }
} 
