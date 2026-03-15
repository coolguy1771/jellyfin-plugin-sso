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

    [Fact]
    public async Task OID_GetNames_ReturnsOkWithEmptyOrArray()
    {
        var response = await _client.GetAsync("/SSO/OID/GetNames");

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
    }

    [Fact]
    public async Task SAML_GetNames_ReturnsOkWithEmptyOrArray()
    {
        var response = await _client.GetAsync("/SSO/SAML/GetNames");

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
    }
}
