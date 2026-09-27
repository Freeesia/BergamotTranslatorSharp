using Xunit;

namespace BergamotTranslatorSharp.Tests;

public sealed class DictionaryTranslationTests
{
    [Fact]
    public void Prepare_EscapesTextAndAssignsAnIdToEachOccurrence()
    {
        var entries = DictionaryTranslation.Validate(new Dictionary<string, string>
        {
            ["AT&T"] = "AT＆T",
            ["<tag>"] = "タグ",
        });

        var prepared = DictionaryTranslation.Prepare("AT&T > <tag> & AT&T", entries);

        Assert.Equal(
            "<bts-term data-id=\"0\">AT&amp;T</bts-term> &gt; " +
            "<bts-term data-id=\"1\">&lt;tag&gt;</bts-term> &amp; " +
            "<bts-term data-id=\"2\">AT&amp;T</bts-term>",
            prepared.Html);
        Assert.Equal(["AT＆T", "タグ", "AT＆T"], prepared.Replacements);
    }

    [Fact]
    public void Prepare_PrefersLongerOverlappingKeys()
    {
        var entries = DictionaryTranslation.Validate(new Dictionary<string, string>
        {
            ["York"] = "ヨーク",
            ["New York"] = "ニューヨーク",
            ["ab"] = "短い",
            ["bcd"] = "長い",
        });

        var prepared = DictionaryTranslation.Prepare("New York and abcd", entries);

        Assert.Equal(
            "<bts-term data-id=\"0\">New York</bts-term> and a" +
            "<bts-term data-id=\"1\">bcd</bts-term>",
            prepared.Html);
        Assert.Equal(["ニューヨーク", "長い"], prepared.Replacements);
    }

    [Fact]
    public void Prepare_HandlesAdjacentJapaneseTextWithoutAddedSpaces()
    {
        var entries = DictionaryTranslation.Validate(new Dictionary<string, string>
        {
            ["東京都"] = "東京",
        });

        var prepared = DictionaryTranslation.Prepare("私は東京都民です", entries);

        Assert.Equal("私は<bts-term data-id=\"0\">東京都</bts-term>民です", prepared.Html);
    }

    [Fact]
    public void Prepare_EscapesPlainTextEvenWhenNoTermMatches()
    {
        var entries = DictionaryTranslation.Validate(new Dictionary<string, string>
        {
            ["missing"] = "unused",
        });

        var prepared = DictionaryTranslation.Prepare("Fish & chips <tag> > cheese.", entries);

        Assert.Equal("Fish &amp; chips &lt;tag&gt; &gt; cheese.", prepared.Html);
        Assert.Empty(prepared.Replacements);
        Assert.Equal("Fish & chips <tag> > cheese.",
            DictionaryTranslation.Restore(prepared.Html, prepared.Replacements));
    }

    [Fact]
    public void Prepare_RejectsEmptyKeys()
    {
        Assert.Throws<ArgumentException>(() => DictionaryTranslation.Validate(
            new Dictionary<string, string> { [""] = "value" }));
    }

    [Fact]
    public void Restore_EmitsSplitAndEmptyTagsOnlyOnce()
    {
        var actual = DictionaryTranslation.Restore(
            "Before <bts-term data-id='0'></bts-term> between " +
            "<bts-term data-id=\"0\">translated</bts-term> and " +
            "<bts-term data-id=\"1\">other</bts-term> after &amp; more",
            ["指定訳", "second"]);

        Assert.Equal("Before 指定訳 between  and second after & more", actual);
    }

    [Fact]
    public void Restore_DoesNotDecodeTheSuppliedTranslation()
    {
        var actual = DictionaryTranslation.Restore(
            "A &amp; <bts-term data-id=\"0\">term</bts-term>",
            ["literal &amp; <value>"]);

        Assert.Equal("A & literal &amp; <value>", actual);
    }

    [Fact]
    public void Restore_HandlesAnEmptySelfClosingTerm()
    {
        var actual = DictionaryTranslation.Restore(
            "Before <bts-term data-id=0/> after", ["指定訳"]);

        Assert.Equal("Before 指定訳 after", actual);
    }

    [Fact]
    public void Restore_RejectsMissingIds()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DictionaryTranslation.Restore("Unmarked translation", ["replacement"]));
    }
}
