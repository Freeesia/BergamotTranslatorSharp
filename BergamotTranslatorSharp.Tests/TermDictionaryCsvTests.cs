using System.Text;
using BergamotTranslatorSharp.Tool;
using Xunit;

namespace BergamotTranslatorSharp.Tests;

public sealed class TermDictionaryCsvTests
{
    [Fact]
    public void Load_ParsesUtf8BomAndQuotedFields()
    {
        var terms = ReadCsv("""
            source,target
            "New,York","ニュー,ヨーク"
            "multi
            line","複数
            行"
            "quote""name","訳""語"
            """);

        Assert.Equal("ニュー,ヨーク", terms["New,York"]);
        Assert.Equal("複数\n行", terms["multi\nline"]);
        Assert.Equal("訳\"語", terms["quote\"name"]);
    }

    [Fact]
    public void Load_AcceptsHeaderlessCaseSensitiveKeys()
    {
        var terms = ReadCsv("AT&T,AT＆T\nat&t,lower");

        Assert.Equal(2, terms.Count);
        Assert.Equal("AT＆T", terms["AT&T"]);
        Assert.Equal("lower", terms["at&t"]);
    }

    [Theory]
    [InlineData("source,target\nHello,one\nHello,two")]
    [InlineData("source,target\n,empty")]
    [InlineData("source,target\nHello,one,extra")]
    [InlineData("source,target\n\"unterminated,one")]
    [InlineData("source,target")]
    public void Load_RejectsInvalidCsv(string csv)
    {
        Assert.Throws<InvalidDataException>(() => ReadCsv(csv));
    }

    private static IReadOnlyDictionary<string, string> ReadCsv(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"bergamot-terms-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, contents, new UTF8Encoding(true));
            return TermDictionaryCsv.Load(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
