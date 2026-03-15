using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using Jellyfin.Plugin.SSO_Auth;
using Xunit;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Unit;

public sealed class SamlResponseTests
{
    [Fact]
    public void Response_LoadXmlFromBase64_DecodesAndLoadsXml()
    {
        var minimalXml = "<root xmlns=\"urn:test\"><child>data</child></root>";
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(minimalXml));
        var certBytes = CreateMinimalTestCert();

        var response = new Response(certBytes, base64);

        response.Xml.Should().Contain("data");
        response.Xml.Should().Contain("child");
    }

    [Fact]
    public void Response_LoadXml_SetsXmlProperty()
    {
        var minimalXml = "<root xmlns=\"urn:test\"/>";
        var certBytes = CreateMinimalTestCert();
        var response = new Response(certBytes);
        response.LoadXml(minimalXml);

        response.Xml.Should().Contain("<root");
    }

    [Fact]
    public void Response_IsValid_WhenNoSignature_ReturnsFalse()
    {
        var xmlNoSignature = @"<samlp:Response xmlns:samlp=""urn:oasis:names:tc:SAML:2.0:protocol""
  xmlns:saml=""urn:oasis:names:tc:SAML:2.0:assertion"" ID=""_id"">
  <saml:Assertion></saml:Assertion>
</samlp:Response>";
        var certBytes = CreateMinimalTestCert();
        var response = new Response(certBytes);
        response.LoadXml(xmlNoSignature);

        response.IsValid().Should().BeFalse();
    }

    private static byte[] CreateMinimalTestCert()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            "CN=Test SAML",
            ecdsa,
            HashAlgorithmName.SHA256);
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return cert.Export(X509ContentType.Cert);
    }
}
