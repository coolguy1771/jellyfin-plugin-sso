using System;
using Duende.IdentityModel.OidcClient;
using FluentAssertions;
using Jellyfin.Plugin.SSO_Auth.Models;
using Xunit;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Unit;

public sealed class AuthModelsTests
{
    [Fact]
    public void TimedAuthorizeState_Constructor_SetsStateAndCreated()
    {
        var state = new AuthorizeState();
        var created = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

        var timed = new TimedAuthorizeState(state, created);

        timed.State.Should().BeSameAs(state);
        timed.Created.Should().Be(created);
        timed.Valid.Should().BeFalse();
        timed.Admin.Should().BeFalse();
        timed.IsLinking.Should().BeFalse();
        timed.EnableLiveTv.Should().BeFalse();
        timed.EnableLiveTvManagement.Should().BeFalse();
        timed.AvatarURL.Should().BeNull();
    }

    [Fact]
    public void AuthResponse_CanSetAndGetProperties()
    {
        var response = new AuthResponse
        {
            DeviceID = "dev-1",
            DeviceName = "Test Device",
            Data = "signed-payload",
        };

        response.DeviceID.Should().Be("dev-1");
        response.DeviceName.Should().Be("Test Device");
        response.Data.Should().Be("signed-payload");
    }
}
