using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Jellyfin.Plugin.SSO_Auth;

/// <summary>
/// A helper class to return HTML for the client's auth flow.
/// Uses embedded HTML/JS templates and injects only escaped values to avoid XSS.
/// </summary>
public static class WebResponse
{
    private const string CallbackBaseResourceName = "Jellyfin.Plugin.SSO_Auth.Views.callbackBase.html";
    private const string CallbackPayloadResourceName = "Jellyfin.Plugin.SSO_Auth.Views.callbackPayload.js";

    private static readonly Lazy<string> BaseTemplate = new Lazy<string>(() => LoadEmbeddedResource(CallbackBaseResourceName));
    private static readonly Lazy<string> PayloadTemplate = new Lazy<string>(() => LoadEmbeddedResource(CallbackPayloadResourceName));

    /// <summary>
    /// Escapes a value for safe use inside a JavaScript single-quoted string.
    /// </summary>
    private static string EscapeForJsString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\'':
                    sb.Append("\\'");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '/':
                    sb.Append("\\/");
                    break;
                case '\u2028':
                    sb.Append("\\u2028");
                    break;
                case '\u2029':
                    sb.Append("\\u2029");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = typeof(WebResponse).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// A generator for the web response that incorporates the data from the server.
    /// </summary>
    /// <param name="data">The data of the auth flow. Is signed XML for SAML and a state ID for OpenID.</param>
    /// <param name="provider">The name of the provider to callback to.</param>
    /// <param name="baseUrl">The base URL of the Jellyfin installation.</param>
    /// <param name="mode">The mode of the function; SAML or OID.</param>
    /// <param name="isLinking">Whether or not this request is to link accounts (Rather than authenticate).</param>
    /// <returns>A string with the HTML to serve to the client.</returns>
    public static string Generator(string data, string provider, string baseUrl, string mode, bool isLinking = false)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || !baseUri.IsAbsoluteUri || string.IsNullOrEmpty(baseUri.Host))
        {
            throw new ArgumentException("Base URL must be a valid absolute URI (e.g. https://host).", nameof(baseUrl));
        }

        var idnMapping = new IdnMapping();
        var punycodeHost = idnMapping.GetAscii(baseUri.Host);
        var builder = new UriBuilder(baseUri)
        {
            Host = punycodeHost,
            Path = baseUri.AbsolutePath,
            Query = baseUri.Query
        };
        var punycodeBaseUrl = builder.Uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

        var authUrl = punycodeBaseUrl + "/sso/" + mode + "/Auth/" + provider;
        var linkUrlPrefix = punycodeBaseUrl + "/sso/" + mode + "/Link/" + provider + "/";

        var appVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion ?? "0.0.0";

        var payload = PayloadTemplate.Value
            .Replace("{{BASE_URL}}", EscapeForJsString(punycodeBaseUrl))
            .Replace("{{DATA}}", EscapeForJsString(data))
            .Replace("{{AUTH_URL}}", EscapeForJsString(authUrl))
            .Replace("{{LINK_URL_PREFIX}}", EscapeForJsString(linkUrlPrefix))
            .Replace("{{IS_LINKING}}", isLinking ? "true" : "false")
            .Replace("{{APP_VERSION}}", EscapeForJsString(appVersion));

        return BaseTemplate.Value.Replace("DYNAMIC_SCRIPT;", payload);
    }
}
