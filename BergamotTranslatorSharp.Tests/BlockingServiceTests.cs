using System.Text.Json.Nodes;
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

    [Fact]
    public void TranslateJson_PreservesJsonStructureAndInnerHtml()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));
        const string json = """{"title":"Hello, world!","items":["<p>How are <strong>you</strong>?</p>","",7,true,null]}""";

        var result = JsonNode.Parse(service.TranslateJson(json));

        Assert.NotEqual("Hello, world!", result?["title"]?.GetValue<string>());
        Assert.Contains("<strong>", result?["items"]?[0]?.GetValue<string>());
        Assert.Equal("", result?["items"]?[1]?.GetValue<string>());
        Assert.Equal(7, result?["items"]?[2]?.GetValue<int>());
        Assert.Equal(true, result?["items"]?[3]?.GetValue<bool>());
        Assert.Null(result?["items"]?[4]);
    }
}
