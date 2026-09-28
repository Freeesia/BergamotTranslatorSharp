using Xunit;

namespace BergamotTranslatorSharp.Tests;

[Collection(nameof(TranslatorModelCollection))]
public sealed class BlockingServiceTests(TranslatorModelFixture models)
{
    private static readonly string[] Inputs =
    [
        "Hello, world!",
        "First line.\nSecond line.",
        "Fish & chips <tag> > cheese.",
        "Café and 東京.",
    ];

    private static readonly string[] HtmlInputs =
    [
        "<p>Hello, <strong>world</strong>!</p>",
        "<p>How are you?</p>",
    ];

    [Fact]
    public void TranslateMultiple_TranslatesEachPlainTextWithOneModel()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));

        var expected = Inputs.Select(input => service.Translate(input)).ToArray();
        var actual = service.Translate(Inputs);

        Assert.Equal(expected, actual);
        Assert.All(actual, translation => Assert.False(string.IsNullOrWhiteSpace(translation)));
        Assert.NotEqual(Inputs[0], actual[0]);
        Assert.Empty(service.Translate(Array.Empty<string>()));
    }

    [Fact]
    public void TranslateMultiple_UsesTwoModelPivot()
    {
        using var service = new BlockingService(
            models.ConfigurationFor("en-kn"),
            models.ConfigurationFor("kn-en"));

        var expected = Inputs.Select(input => service.Translate(input)).ToArray();
        var actual = service.Translate(Inputs);

        Assert.Equal(expected, actual);
        Assert.All(actual, translation => Assert.False(string.IsNullOrWhiteSpace(translation)));
    }

    [Fact]
    public void TranslateMultiple_PreservesHtmlWithOneModel()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));

        var expected = HtmlInputs.Select(input => service.Translate(input, html: true)).ToArray();
        var actual = service.Translate(HtmlInputs, html: true);

        Assert.Equal(expected, actual);
        Assert.Contains("<strong>", actual[0]);
    }

    [Fact]
    public void TranslateMultiple_PreservesHtmlWithTwoModelPivot()
    {
        using var service = new BlockingService(
            models.ConfigurationFor("en-kn"),
            models.ConfigurationFor("kn-en"));

        var expected = HtmlInputs.Select(input => service.Translate(input, html: true)).ToArray();
        var actual = service.Translate(HtmlInputs, html: true);

        Assert.Equal(expected, actual);
        Assert.Contains("<strong>", actual[0]);
    }

    [Fact]
    public void Translate_AppliesDictionaryWithOneModel()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));
        var dictionary = new Dictionary<string, string> { ["world"] = "指定訳" };

        var actual = service.Translate("Hello, world!", dictionary);

        Assert.Contains("指定訳", actual);
        Assert.DoesNotContain("bts-term", actual);
        Assert.DoesNotContain("data-id", actual);
    }

    [Fact]
    public void Translate_AppliesMultipleTermsAndRepeatedOccurrences()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));
        var dictionary = new Dictionary<string, string>
        {
            ["Hello"] = "FIRST",
            ["world"] = "SECOND",
        };

        var actual = service.Translate("Hello world. Hello world.", dictionary);

        Assert.Equal(2, actual.Split("FIRST").Length - 1);
        Assert.Equal(2, actual.Split("SECOND").Length - 1);
        Assert.DoesNotContain("bts-term", actual);
    }

    [Fact]
    public void TranslateHtml_DictionaryTagStaysInlineAndItsTextIsTranslated()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));
        const string openingTag = "<bts-term data-id=\"0\">";

        var expected = service.Translate("Hello world!");
        var actual = service.Translate($"He{openingTag}llo</bts-term> world!", html: true);

        Assert.Contains(openingTag, actual);
        Assert.Equal(expected, actual.Replace(openingTag, "").Replace("</bts-term>", ""));
    }

    [Fact]
    public void Translate_RejectsEmptyDictionaryKeys()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));

        Assert.Throws<ArgumentException>(() => service.Translate(
            "Hello", new Dictionary<string, string> { [""] = "value" }));
    }

    [Fact]
    public void TranslateMultiple_AppliesDictionaryToEachInputInOrder()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));
        var dictionary = new Dictionary<string, string> { ["world"] = "指定訳" };
        var inputs = new[] { "Hello, world!", "No matching term.", "Another world." };

        var expected = inputs.Select(input => service.Translate(input, dictionary)).ToArray();
        var actual = service.Translate(inputs, dictionary);

        Assert.Equal(expected, actual);
        Assert.Contains("指定訳", actual[0]);
        Assert.DoesNotContain("指定訳", actual[1]);
        Assert.Contains("指定訳", actual[2]);
    }

    [Fact]
    public void TranslateMultiple_AppliesDictionaryWithTwoModelPivot()
    {
        using var service = new BlockingService(
            models.ConfigurationFor("en-kn"),
            models.ConfigurationFor("kn-en"));
        var dictionary = new Dictionary<string, string> { ["world"] = "glossary term" };
        var inputs = new[] { "Hello, world!", "Another world." };

        var actual = service.Translate(inputs, dictionary);

        Assert.Equal(inputs.Length, actual.Length);
        Assert.All(actual, translation => Assert.Contains("glossary term", translation));
        Assert.All(actual, translation => Assert.DoesNotContain("bts-term", translation));
    }
}
