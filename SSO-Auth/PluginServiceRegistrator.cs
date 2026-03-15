using Jellyfin.Plugin.SSO_Auth.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.SSO_Auth;

/// <summary>
/// Registers plugin services with the Jellyfin host so SSOController can receive IOidStateStore via DI.
/// </summary>
public sealed class SSOPluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<IOidStateStore>(OidStateStore.Instance);
    }
}
