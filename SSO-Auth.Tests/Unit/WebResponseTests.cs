using FluentAssertions;
using Jellyfin.Plugin.SSO_Auth;
using Xunit;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Unit;

public sealed class WebResponseTests
{
    [Fact]
    public void Generator_ReturnsHtmlContainingEscapedData()
    {
        var data = "payload with 'quote' and \\backslash";
        var html = WebResponse.Generator(data, "p1", "https://jellyfin.example", "OID", false);

        html.Should().NotContain("{{DYNAMIC_SCRIPT}}");
        html.Should().Contain("\\\\");
        html.Should().Contain("\\'");
        html.Should().NotContain("payload with 'quote'");
    }

    [Fact]
    public void Generator_IncludesAuthUrlAndLinkUrlPrefix()
    {
        var html = WebResponse.Generator("d", "myprov", "https://host.example", "SAML", false);

        html.Should().Contain("/sso/SAML/Auth/myprov");
        html.Should().Contain("/sso/SAML/Link/myprov/");
    }

    [Theory]
    [InlineData(false, "false")]
    [InlineData(true, "true")]
    public void Generator_ReplacesIsLinkingCorrectly(bool isLinking, string expected)
    {
        var html = WebResponse.Generator("x", "p", "https://h.example", "OID", isLinking);

        html.Should().Contain(expected);
    }

    [Fact]
    public void Generator_ThrowsWhenBaseUrlEmpty()
    {
        var act = () => WebResponse.Generator("d", "p", string.Empty, "OID");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("baseUrl");
    }

    [Fact]
    public void Generator_ThrowsWhenBaseUrlHasNoProtocolSeparator()
    {
        var act = () => WebResponse.Generator("d", "p", "not-a-url", "OID");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("baseUrl");
    }

    [Fact]
    public void Generator_EmbedsBaseUrlInOutput()
    {
        var baseUrl = "https://jellyfin.test";
        var html = WebResponse.Generator("data", "prov", baseUrl, "OID", false);

        html.Should().Contain("https://jellyfin.test");
    }
}
