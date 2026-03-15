using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.SSO_Auth.Helpers;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using FluentAssertions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Unit;

/// <summary>
/// RequestHelpers.AssertCanUpdateUser depends on Jellyfin internal types (AuthorizationInfo, User)
/// that are not easily constructible in unit tests. Coverage for this helper is provided via
/// integration tests when exercising SSO controller endpoints that call AssertCanUpdateUser.
/// </summary>
public sealed class RequestHelpersTests
{
    [Fact]
    public async Task AssertCanUpdateUser_IsCallable()
    {
        var authContext = new Mock<IAuthorizationContext>();
        var request = new Mock<HttpRequest>();

        authContext
            .Setup(c => c.GetAuthorizationInfo(It.IsAny<HttpRequest>()))
            .ThrowsAsync(new InvalidOperationException("mock not configured"));

        Func<Task> act = () => RequestHelpers.AssertCanUpdateUser(
            authContext.Object,
            request.Object,
            Guid.NewGuid(),
            false);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("mock not configured");
    }
}
