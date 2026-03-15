using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.SSO_Auth.Models;

namespace Jellyfin.Plugin.SSO_Auth.Services;

/// <summary>
/// In-memory store for OIDC authorization state with expiration.
/// </summary>
public sealed class OidStateStore : IOidStateStore
{
    private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(1);
    private readonly object _lock = new object();
    private readonly Dictionary<string, TimedAuthorizeState> _states = new Dictionary<string, TimedAuthorizeState>();

    /// <summary>
    /// Singleton instance for use when the host does not provide IOidStateStore via DI.
    /// </summary>
    public static readonly IOidStateStore Instance = new OidStateStore();

    /// <inheritdoc />
    public bool TryGetValue(string state, out TimedAuthorizeState timedState)
    {
        lock (_lock)
        {
            return _states.TryGetValue(state, out timedState);
        }
    }

    /// <inheritdoc />
    public void Add(string key, TimedAuthorizeState value)
    {
        lock (_lock)
        {
            _states[key] = value;
        }
    }

    /// <inheritdoc />
    public bool Remove(string key)
    {
        lock (_lock)
        {
            return _states.Remove(key);
        }
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            var toRemove = _states
                .Where(kvp => now.Subtract(kvp.Value.Created) > Expiration)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in toRemove)
            {
                _states.Remove(key);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, TimedAuthorizeState> GetSnapshot()
    {
        lock (_lock)
        {
            return new Dictionary<string, TimedAuthorizeState>(_states);
        }
    }
}
