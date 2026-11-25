
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

    private string GenerateJwtToken(string userId)
    {
        var key = _validationParameters.IssuerSigningKey
            ?? throw new InvalidOperationException(
                "JWT signing key not found.");

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
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

    // Test: Get note by ID with valid token
    [Fact]
    public async Task Get_Note_By_Id_With_Valid_Token_Should_Return_Ok()
    {
        // Arrange
        var token = GenerateJwtToken("1002");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Create a note
        var createResponse = await _client.PostAsJsonAsync(
            "/api/Notes",
            new
            {
                Title = "Integration Test Note",
                Content = "Testing GET note by ID",
                Topic = "Testing"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdResponse =
            await createResponse.Content
                .ReadFromJsonAsync<ApiResponse<CreateNoteResponse>>();

        Assert.NotNull(createdResponse);
        Assert.NotNull(createdResponse.Data);

        // Act
        var response = await _client.GetAsync(
            $"/api/Notes/{createdResponse.Data.NoteId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task User_Cannot_Access_Another_Users_Note()
    {
        // User 1002 creates a note
        var user1Token = GenerateJwtToken("1002");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user1Token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/Notes",
            new
            {
                Title = "Private Note",
                Content = "This belongs to User 1002",
                Topic = "Security"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdResponse =
            await createResponse.Content
                .ReadFromJsonAsync<ApiResponse<CreateNoteResponse>>();

        Assert.NotNull(createdResponse);
        Assert.NotNull(createdResponse.Data);

        // User 2002 tries to access User 1002's note
        var user2Token = GenerateJwtToken("2002");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user2Token);

        var response = await _client.GetAsync(
            $"/api/Notes/{createdResponse.Data.NoteId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task User_Cannot_Update_Another_Users_Note()
    {
        // User 1002 creates a note
        var user1Token = GenerateJwtToken("1002");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user1Token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/Notes",
            new
            {
                Title = "Private Note",
                Content = "This belongs to User 1002",
                Topic = "Security"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdResponse =
            await createResponse.Content
                .ReadFromJsonAsync<ApiResponse<CreateNoteResponse>>();

        Assert.NotNull(createdResponse);
        Assert.NotNull(createdResponse.Data);

        // User 2002 tries to update User 1002's note
        var user2Token = GenerateJwtToken("2002");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user2Token);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/Notes/{createdResponse.Data.NoteId}",
            new
            {
                Title = "Hacked Note",
                Content = "Trying to update another user's note",
                Topic = "Unauthorized"
            });

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    [Fact]
    public async Task User_Cannot_Delete_Another_Users_Note()
    {
        // User 1002 creates a note
        var user1Token = GenerateJwtToken("1002");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user1Token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/Notes",
            new
            {
                Title = "Private Note",
                Content = "This belongs to User 1002",
                Topic = "Security"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdResponse =
            await createResponse.Content
                .ReadFromJsonAsync<ApiResponse<CreateNoteResponse>>();

        Assert.NotNull(createdResponse);
        Assert.NotNull(createdResponse.Data);

        // User 2002 tries to delete User 1002's note
        var user2Token = GenerateJwtToken("2002");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", user2Token);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/Notes/{createdResponse.Data.NoteId}");

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    // Standardized API response envelope
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
