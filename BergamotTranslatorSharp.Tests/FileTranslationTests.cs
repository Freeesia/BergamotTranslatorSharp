using BergamotTranslatorSharp.Tool;
using Xunit;

namespace BergamotTranslatorSharp.Tests;

public sealed class FileTranslationTests
{
    [Theory]
    [InlineData("input.json", null, "Json")]
    [InlineData("input.json5", null, "Json5")]
    [InlineData("input.yaml", null, "Yaml")]
    [InlineData("input.yml", null, "Yaml")]
    [InlineData("input.toml", null, "Toml")]
    [InlineData("input.ini", null, "Ini")]
    [InlineData("input.cbor", null, "Cbor")]
    [InlineData("input.msgpack", null, "MessagePack")]
    [InlineData("input.mpk", null, "MessagePack")]
    public void ResolveFormat_UsesSupportedFileExtensions(string path, string? format, string expected)
    {
        Assert.Equal(expected, FileTranslation.ResolveFormat(path, format).ToString());
    }

    [Theory]
    [InlineData("anything.bin", "json", "Json")]
    [InlineData("anything.conf", "yml", "Yaml")]
    [InlineData("anything.dat", "msgpack", "MessagePack")]
    public void ResolveFormat_ExplicitFormatOverridesExtension(string path, string format, string expected)
    {
        Assert.Equal(expected, FileTranslation.ResolveFormat(path, format).ToString());
    }

    [Theory]
    [InlineData("input.bin", null)]
    [InlineData("input.dat", null)]
    [InlineData("input.conf", null)]
    [InlineData("input.cfg", null)]
    [InlineData("input.csv", null)]
    [InlineData("input.xml", null)]
    [InlineData("input.html", null)]
    [InlineData("input.json", "xml")]
    public void ResolveFormat_RejectsUnknownOrAmbiguousFormats(string path, string? format)
    {
        Assert.Throws<ArgumentException>(() => FileTranslation.ResolveFormat(path, format));
    }
}
