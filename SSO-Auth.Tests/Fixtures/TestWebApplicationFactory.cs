using System.Collections.Concurrent;
using System.Reflection;
using Jellyfin.Plugin.SSO_Auth;
using Jellyfin.Plugin.SSO_Auth.Config;
using Jellyfin.Plugin.SSO_Auth.Services;
using Jellyfin.Plugin.SSO_Auth.Api;
using Jellyfin.Plugin.SSO_Auth.Views;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Fixtures;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly ConcurrentBag<string> _tempDirs = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddControllers()
                .AddApplicationPart(typeof(SSOController).Assembly)
                .AddApplicationPart(typeof(SSOViewsController).Assembly);

            services.AddSingleton(_ => CreateMockLogger<SSOController>());
            services.AddSingleton(_ => CreateMockLoggerFactory());
            services.AddSingleton(_ => Mock.Of<ISessionManager>());
            services.AddSingleton(_ => Mock.Of<IUserManager>());
            services.AddSingleton(_ => Mock.Of<IAuthorizationContext>());
            services.AddSingleton(_ => Mock.Of<ICryptoProvider>());
            services.AddSingleton(_ => Mock.Of<IProviderManager>());
            services.AddSingleton(_ => Mock.Of<IHttpClientFactory>());
            services.AddSingleton(_ => Mock.Of<IServerConfigurationManager>());
            services.AddSingleton<IOidStateStore>(_ => new OidStateStore());

            services.AddSingleton<IStartupFilter>(_ => new SetSSOPluginInstanceStartupFilter(_tempDirs));
        });
    }

    private static ILogger<T> CreateMockLogger<T>()
    {
        var logger = new Mock<ILogger<T>>();
        return logger.Object;
    }

    private static ILoggerFactory CreateMockLoggerFactory()
    {
        var factory = new Mock<ILoggerFactory>();
        var logger = new Mock<ILogger<SSOController>>();
        factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        return factory.Object;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var dir in _tempDirs)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                }
                catch
                {
                    // ignore cleanup errors
                }
            }

            _tempDirs.Clear();
        }

        base.Dispose(disposing);
    }

    private sealed class SetSSOPluginInstanceStartupFilter : IStartupFilter
    {
        private readonly ConcurrentBag<string> _tempDirs;

        public SetSSOPluginInstanceStartupFilter(ConcurrentBag<string> tempDirs)
        {
            _tempDirs = tempDirs;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var suffix = Guid.NewGuid().ToString("N");
                var pluginsPath = Path.Combine(Path.GetTempPath(), "jellyfin-sso-test-" + suffix);
                var configPath = Path.Combine(Path.GetTempPath(), "jellyfin-sso-test-config-" + suffix);
                Directory.CreateDirectory(pluginsPath);
                Directory.CreateDirectory(configPath);
                _tempDirs.Add(pluginsPath);
                _tempDirs.Add(configPath);

                var paths = new Mock<IApplicationPaths>();
                paths.Setup(p => p.PluginsPath).Returns(pluginsPath);
                paths.Setup(p => p.PluginConfigurationsPath).Returns(configPath);

                var serializer = new Mock<IXmlSerializer>();
                serializer.Setup(s => s.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>()))
                    .Returns((Type _, string _) => new PluginConfiguration());

                var plugin = new SSOPlugin(paths.Object, serializer.Object);
                var instanceProp = typeof(SSOPlugin).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp == null || !instanceProp.CanWrite)
                {
                    throw new InvalidOperationException(
                        "SSOPlugin.Instance property not found or not writable; test setup depends on it.");
                }
                instanceProp.SetValue(null, plugin);

                next(app);
            };
        }
    }
}
