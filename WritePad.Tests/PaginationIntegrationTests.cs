
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using WritePad.DTOs;
using WritePad.Tests.Infrastructure;

using Xunit;

namespace WritePad.Tests;

public class PaginationIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TokenValidationParameters _validationParameters;

    public PaginationIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        var jwtOptions = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        _validationParameters = jwtOptions.TokenValidationParameters;
    }

    private string GenerateJwtToken(int userId = 1002)
    {
        var key = _validationParameters.IssuerSigningKey
            ?? throw new InvalidOperationException(
                "JWT signing key not found.");

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                userId.ToString()),

            new Claim(
                ClaimTypes.Name,
                "testuser")
        };

        var token = new JwtSecurityToken(
            issuer: _validationParameters.ValidIssuer,
            audience: _validationParameters.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private async Task<int> CreateNoteAsync(
        int userId,
        string title)
    {
        var token = GenerateJwtToken(userId);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/Notes");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(
            new CreateNoteRequestDto
            {
                Title = title,
                Content = "Pagination integration test content",
                Topic = "Testing"
            });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<ApiResponse<NoteResponseDto>>();

        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);

        return body.Data.NoteId;
    }

    [Fact]
    public async Task Get_Notes_Should_Return_Paginated_Results()
    {
        // Arrange: Create five notes for user 1002.
        for (int i = 1; i <= 5; i++)
        {
            await CreateNoteAsync(
                1002,
                $"Pagination Test Note {Guid.NewGuid()}");
        }

        var token = GenerateJwtToken(1002);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Notes?pageNumber=1&pageSize=2");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<
                ApiResponse<PagedResponse<NoteResponseDto>>>();

        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);

        Assert.Equal(1, body.Data.PageNumber);
        Assert.Equal(2, body.Data.PageSize);

        Assert.Equal(2, body.Data.Items.Count);

        Assert.True(body.Data.TotalRecords >= 5);

        var expectedTotalPages =
            (int)Math.Ceiling(
                (double)body.Data.TotalRecords / 2);

        Assert.Equal(
            expectedTotalPages,
            body.Data.TotalPages);
    }

    [Fact]
    public async Task Get_Notes_With_Invalid_PageNumber_Should_Return_BadRequest()
    {
        // Arrange
        var token = GenerateJwtToken();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Notes?pageNumber=0&pageSize=5");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_Notes_With_Invalid_PageSize_Should_Return_BadRequest()
    {
        // Arrange
        var token = GenerateJwtToken();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Notes?pageNumber=1&pageSize=200");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Get_Notes_Should_Return_Only_Authenticated_Users_Notes()
    {
        // Arrange: Create one note for another user.
        int otherUserNoteId = await CreateNoteAsync(
            2002,
            $"Other User Note {Guid.NewGuid()}");

        var token = GenerateJwtToken(1002);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Notes?pageNumber=1&pageSize=100");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<
                ApiResponse<PagedResponse<NoteResponseDto>>>();

        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);

        Assert.DoesNotContain(
            body.Data.Items,
            note => note.NoteId == otherUserNoteId);

        Assert.All(
            body.Data.Items,
            note => Assert.Equal(1002, note.UserId));
    }
}
