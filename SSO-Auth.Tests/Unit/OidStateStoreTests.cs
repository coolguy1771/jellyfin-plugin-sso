using System;
using Duende.IdentityModel.OidcClient;
using FluentAssertions;
using Jellyfin.Plugin.SSO_Auth.Models;
using Jellyfin.Plugin.SSO_Auth.Services;
using Xunit;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Unit;

public sealed class OidStateStoreTests
{
    [Fact]
    public void Add_ThenTryGetValue_ReturnsTrueAndState()
    {
        var store = new OidStateStore();
        var state = new AuthorizeState();
        var timed = new TimedAuthorizeState(state, DateTime.UtcNow);
        store.Add("key1", timed);

        store.TryGetValue("key1", out var outVal).Should().BeTrue();
        outVal.Should().BeSameAs(timed);
    }

    [Fact]
    public void TryGetValue_WhenKeyMissing_ReturnsFalse()
    {
        var store = new OidStateStore();

        store.TryGetValue("missing", out var outVal).Should().BeFalse();
        outVal.Should().BeNull();
    }

    [Fact]
    public void Remove_WhenKeyExists_ReturnsTrueAndRemoves()
    {
        var store = new OidStateStore();
        var timed = new TimedAuthorizeState(new AuthorizeState(), DateTime.UtcNow);
        store.Add("key1", timed);

        store.Remove("key1").Should().BeTrue();
        store.TryGetValue("key1", out _).Should().BeFalse();
    }

    [Fact]
    public void Remove_WhenKeyMissing_ReturnsFalse()
    {
        var store = new OidStateStore();

        store.Remove("missing").Should().BeFalse();
    }

    [Fact]
    public void Add_ReplacesExistingValue()
    {
        var store = new OidStateStore();
        var first = new TimedAuthorizeState(new AuthorizeState(), DateTime.UtcNow);
        var second = new TimedAuthorizeState(new AuthorizeState(), DateTime.UtcNow);
        store.Add("key1", first);
        store.Add("key1", second);

        store.TryGetValue("key1", out var outVal).Should().BeTrue();
        outVal.Should().BeSameAs(second);
    }

    [Fact]
    public void GetSnapshot_ReturnsCopyOfCurrentStates()
    {
        var store = new OidStateStore();
        var timed = new TimedAuthorizeState(new AuthorizeState(), DateTime.UtcNow);
        store.Add("key1", timed);

        var snapshot = store.GetSnapshot();
        snapshot.Should().ContainKey("key1");
        snapshot["key1"].Should().BeSameAs(timed);
        snapshot.Should().HaveCount(1);

        store.Remove("key1");
        store.GetSnapshot().Should().BeEmpty();
        snapshot.Count.Should().Be(1);
    }

    [Fact]
    public void Invalidate_RemovesExpiredEntries()
    {
        var store = new OidStateStore();
        var expired = new TimedAuthorizeState(
            new AuthorizeState(),
            DateTime.Now.AddMinutes(-5));
        var valid = new TimedAuthorizeState(
            new AuthorizeState(),
            DateTime.Now);
        store.Add("expired", expired);
        store.Add("valid", valid);

        store.Invalidate();

        store.TryGetValue("expired", out _).Should().BeFalse();
        store.TryGetValue("valid", out var v).Should().BeTrue();
        v.Should().BeSameAs(valid);
    }
}
