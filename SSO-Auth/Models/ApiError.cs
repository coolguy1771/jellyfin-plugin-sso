namespace Jellyfin.Plugin.SSO_Auth.Models;

/// <summary>
/// Structured error response for API clients.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Gets or sets the error code for client handling.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets the human-readable message.
    /// </summary>
    public string Message { get; set; }
}
