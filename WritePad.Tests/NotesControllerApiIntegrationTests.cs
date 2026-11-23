
using System.Net;
using Xunit;
using WritePad.Tests.Infrastructure;

namespace WritePad.Tests;

public class NotesControllerApiIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public NotesControllerApiIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Notes_Without_Token_Should_Return_Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/Notes");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
} 
