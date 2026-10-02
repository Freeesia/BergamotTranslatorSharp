using BergamotTranslatorSharp.Tool;
using Xunit;

namespace BergamotTranslatorSharp.Tests;

public sealed class FileTranslationTests
{
    [Theory]
    [InlineData("input.json", "Json")]
    [InlineData("input.json5", "Json5")]
    [InlineData("input.yaml", "Yaml")]
    [InlineData("input.yml", "Yaml")]
    [InlineData("input.toml", "Toml")]
    [InlineData("input.ini", "Ini")]
    [InlineData("input.cbor", "Cbor")]
    [InlineData("input.msgpack", "MessagePack")]
    [InlineData("input.mpk", "MessagePack")]
    public void ResolveFormat_UsesSupportedFileExtensions(string path, string expected)
    {
        Assert.Equal(expected, FileTranslation.ResolveFormat(path).ToString());
    }

    [Theory]
    [InlineData("input.bin")]
    [InlineData("input.dat")]
    [InlineData("input.conf")]
    [InlineData("input.cfg")]
    [InlineData("input.csv")]
    [InlineData("input.xml")]
    [InlineData("input.html")]
    [InlineData("input.htm")]
    public void ResolveFormat_RejectsUnknownOrAmbiguousFormats(string path)
    {
        Assert.Throws<ArgumentException>(() => FileTranslation.ResolveFormat(path));
    }
}
