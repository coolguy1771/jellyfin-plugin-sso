using System.IO;
using System.Xml;
using System.Xml.Serialization;
using FluentAssertions;
using Jellyfin.Plugin.SSO_Auth;
using Xunit;

namespace Jellyfin.Plugin.SSO_Auth.Tests.Unit;

public sealed class SerializableDictionaryTests
{
    [Fact]
    public void SerializableDictionary_RoundTripsViaXml_StringString()
    {
        var dict = new SerializableDictionary<string, string>
        {
            ["a"] = "1",
            ["b"] = "2",
        };

        var serialized = Serialize(dict);
        var deserialized = Deserialize<string, string>(serialized);

        deserialized.Should().HaveCount(2);
        deserialized["a"].Should().Be("1");
        deserialized["b"].Should().Be("2");
    }

    private static string Serialize<TKey, TValue>(SerializableDictionary<TKey, TValue> dict)
    {
        var serializer = new XmlSerializer(typeof(SerializableDictionary<TKey, TValue>));
        using var sw = new StringWriter();
        using var writer = XmlWriter.Create(sw);
        serializer.Serialize(writer, dict);
        return sw.ToString();
    }

    private static SerializableDictionary<TKey, TValue> Deserialize<TKey, TValue>(string xml)
    {
        var serializer = new XmlSerializer(typeof(SerializableDictionary<TKey, TValue>));
        using var sr = new StringReader(xml);
        using var reader = XmlReader.Create(sr);
        return (SerializableDictionary<TKey, TValue>)serializer.Deserialize(reader)!;
    }
}
