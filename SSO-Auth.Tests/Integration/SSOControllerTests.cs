using System.Text.Json;
using FluentAssertions;
using Jellyfin.Plugin.SSO_Auth.Tests.Fixtures;
using Xunit;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Integration;

public sealed class SSOControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SSOControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/SSO/OID/GetNames")]
    [InlineData("/SSO/SAML/GetNames")]
    public async Task GetNames_ReturnsOkWithEmptyOrArray(string route)
    {
        var response = await _client.GetAsync(route);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
        var names = JsonSerializer.Deserialize<string[]>(content);
        names.Should().NotBeNull().And.BeAssignableTo<string[]>();
    }
}
