using Xunit;

namespace BergamotTranslatorSharp.Tests;

public sealed class BlockingServiceTests(TranslatorModelFixture models) : IClassFixture<TranslatorModelFixture>
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
}
