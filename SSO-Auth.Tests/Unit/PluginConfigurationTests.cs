using FluentAssertions;
using Jellyfin.Plugin.SSO_Auth.Config;
using Xunit;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Unit;

public sealed class PluginConfigurationTests
{
    [Fact]
    public void PluginConfiguration_Constructor_SetsDefaultCollections()
    {
        var config = new PluginConfiguration();

        config.SamlConfigs.Should().NotBeNull();
        config.SamlConfigs.Should().BeEmpty();
        config.OidConfigs.Should().NotBeNull();
        config.OidConfigs.Should().BeEmpty();
    }

    [Fact]
    public void SamlConfig_CanSetAndGetProperties()
    {
        var config = new SamlConfig
        {
            SamlEndpoint = "https://idp.example/metadata",
            Enabled = true,
            EnableAllFolders = true,
        };

        config.SamlEndpoint.Should().Be("https://idp.example/metadata");
        config.Enabled.Should().BeTrue();
        config.EnableAllFolders.Should().BeTrue();
        config.CanonicalLinks.Should().NotBeNull();
        config.CanonicalLinks.Should().BeEmpty();
    }

    [Fact]
    public void OidConfig_CanSetAndGetProperties()
    {
        var config = new OidConfig
        {
            OidEndpoint = "https://oid.example/.well-known/openid-configuration",
            Enabled = true,
        };

        config.OidEndpoint.Should().Be("https://oid.example/.well-known/openid-configuration");
        config.Enabled.Should().BeTrue();
        config.CanonicalLinks.Should().NotBeNull();
        config.CanonicalLinks.Should().BeEmpty();
    }
}
