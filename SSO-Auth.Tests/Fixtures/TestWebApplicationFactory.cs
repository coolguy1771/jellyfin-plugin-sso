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

            services.AddSingleton<IStartupFilter, SetSSOPluginInstanceStartupFilter>();
        });
    }

    private static ILogger<T> CreateMockLogger<T>()
    {
        var factory = new Mock<ILoggerFactory>();
        var logger = new Mock<ILogger<T>>();
        factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        return logger.Object;
    }

    private static ILoggerFactory CreateMockLoggerFactory()
    {
        var factory = new Mock<ILoggerFactory>();
        var logger = new Mock<ILogger<SSOController>>();
        factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        return factory.Object;
    }

    private sealed class SetSSOPluginInstanceStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var paths = new Mock<IApplicationPaths>();
                paths.Setup(p => p.PluginsPath).Returns(Path.Combine(Path.GetTempPath(), "jellyfin-sso-test"));
                paths.Setup(p => p.PluginConfigurationsPath).Returns(Path.Combine(Path.GetTempPath(), "jellyfin-sso-test-config"));

                var serializer = new Mock<IXmlSerializer>();
                serializer.Setup(s => s.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>()))
                    .Returns((Type _, string _) => new PluginConfiguration());

                var plugin = new SSOPlugin(paths.Object, serializer.Object);
                typeof(SSOPlugin).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)!
                    .SetValue(null, plugin);

                next(app);
            };
        }
    }
}
