
using System.Net;
using System.Net.Http.Headers;
using Xunit;
using WritePad.Tests.Infrastructure;

namespace WritePad.Tests;

public class ApiTestInvalidJwt
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTestInvalidJwt(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Notes_With_Invalid_Token_Should_Return_Unauthorized()
    {
        // Arrange
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/Notes"
        );

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode
        );
    }
}
