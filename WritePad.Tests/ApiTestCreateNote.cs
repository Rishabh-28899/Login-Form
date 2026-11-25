
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

        // Assert 1: Resource creation should return 201 Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Read standardized API response
        var responseBody =
            await response.Content.ReadFromJsonAsync<ApiResponse<CreateNoteResponse>>();

        Assert.NotNull(responseBody);
        Assert.True(responseBody.Success);
        Assert.NotNull(responseBody.Data);

        int noteId = responseBody.Data.NoteId;
        int userId = responseBody.Data.UserId;

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

    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    private class CreateNoteResponse
    {
        public int NoteId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}
