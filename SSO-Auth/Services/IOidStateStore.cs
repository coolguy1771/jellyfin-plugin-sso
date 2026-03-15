using System.Collections.Generic;
using Jellyfin.Plugin.SSO_Auth.Models;

namespace Jellyfin.Plugin.SSO_Auth.Services;

/// <summary>
/// Stores and expires OIDC authorization state for the duration of the login flow.
/// </summary>
public interface IOidStateStore
{
    /// <summary>
    /// Tries to get the timed state for the given state key.
    /// </summary>
    /// <param name="state">The state key to look up.</param>
    /// <param name="timedState">When this method returns, contains the state if found; otherwise null.</param>
    /// <returns>True if the state was found; otherwise false.</returns>
    bool TryGetValue(string state, out TimedAuthorizeState timedState);

    /// <summary>
    /// Adds a new state. Replaces existing if the key already exists.
    /// </summary>
    /// <param name="key">The state key.</param>
    /// <param name="value">The timed authorize state to store.</param>
    void Add(string key, TimedAuthorizeState value);

    /// <summary>
    /// Removes the state for the given key.
    /// </summary>
    /// <param name="key">The state key to remove.</param>
    /// <returns>True if the state was removed; otherwise false.</returns>
    bool Remove(string key);

    /// <summary>
    /// Removes states older than the configured expiration (e.g. 1 minute).
    /// </summary>
    void Invalidate();

    /// <summary>
    /// Returns a snapshot of all current states (for debug endpoint).
    /// </summary>
    /// <returns>A read-only copy of the current state dictionary.</returns>
    IReadOnlyDictionary<string, TimedAuthorizeState> GetSnapshot();
}
