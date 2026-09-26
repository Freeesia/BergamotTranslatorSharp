using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace BergamotTranslatorSharp.Tests;

public sealed class JsonHtmlTranslationConverterTests
{
    [Fact]
    public void RoundTrip_PreservesStructureAndSkipsWhitespaceValues()
    {
        const string json = """
            {"title":"Hello","nested":{"items":["World",42,true,null,"", "  \t  "]},"count":2.5}
            """;

        var context = JsonHtmlTranslationConverter.Instance.ToHtml(json);
        var translatedHtml = context.Html.Replace(">Hello<", ">Bonjour<")
            .Replace(">World<", ">Monde<");
        var result = JsonNode.Parse(context.FromHtml(translatedHtml));

        Assert.True(context.HasTranslatableContent);
        Assert.Equal("Bonjour", result?["title"]?.GetValue<string>());
        Assert.Equal("Monde", result?["nested"]?["items"]?[0]?.GetValue<string>());
        Assert.Equal(42, result?["nested"]?["items"]?[1]?.GetValue<int>());
        Assert.Equal(true, result?["nested"]?["items"]?[2]?.GetValue<bool>());
        Assert.Null(result?["nested"]?["items"]?[3]);
        Assert.Equal("", result?["nested"]?["items"]?[4]?.GetValue<string>());
        Assert.Equal("  \t  ", result?["nested"]?["items"]?[5]?.GetValue<string>());
        Assert.Equal(2.5, result?["count"]?.GetValue<double>());
        Assert.Null(result?["Bonjour"]);
    }

    [Fact]
    public void RoundTrip_UsesHtmlInsideStringValues()
    {
        const string json = """{"html":"<p>Hello, <strong>world</strong>!</p>","text":"5 < 6 & tea","entity":"&amp;"}""";
        var context = JsonHtmlTranslationConverter.Instance.ToHtml(json);

        Assert.Contains("<strong>world</strong>", context.Html);
        Assert.Contains("5 &lt; 6 &amp; tea", context.Html);

        var translatedHtml = context.Html.Replace("Hello", "Bonjour")
            .Replace("world", "monde");
        var result = JsonNode.Parse(context.FromHtml(translatedHtml));

        Assert.Equal("<p>Bonjour, <strong>monde</strong>!</p>", result?["html"]?.GetValue<string>());
        Assert.Equal("5 < 6 & tea", result?["text"]?.GetValue<string>());
        Assert.Equal("&amp;", result?["entity"]?.GetValue<string>());
    }

    [Fact]
    public void RoundTrip_RestoresRootStringAndNoContentJson()
    {
        var stringContext = JsonHtmlTranslationConverter.Instance.ToHtml("\"Hello\"");
        Assert.Equal("\"Bonjour\"", stringContext.FromHtml(stringContext.Html.Replace("Hello", "Bonjour")));

        foreach (var json in new[] { "null", "123", "true", "[]", "{\"empty\":\"\",\"space\":\" \"}" })
        {
            var context = JsonHtmlTranslationConverter.Instance.ToHtml(json);
            Assert.False(context.HasTranslatableContent);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(context.FromHtml(context.Html))));
        }
    }

    [Fact]
    public void FromHtml_UsesIdentifiersInsteadOfElementOrder()
    {
        var context = JsonHtmlTranslationConverter.Instance.ToHtml("[\"first\",\"second\"]");
        const string reversed = """
            <div data-bergamot-json-root="1"><div data-bergamot-json-id="1">deuxième</div><div data-bergamot-json-id="0">premier</div></div>
            """;

        var result = JsonNode.Parse(context.FromHtml(reversed));
        Assert.Equal("premier", result?[0]?.GetValue<string>());
        Assert.Equal("deuxième", result?[1]?.GetValue<string>());
    }

    [Theory]
    [InlineData("<div data-bergamot-json-root=\"1\"><div data-bergamot-json-id=\"0\">one</div></div>")]
    [InlineData("<div data-bergamot-json-root=\"1\"><div data-bergamot-json-id=\"0\">one</div><div data-bergamot-json-id=\"0\">two</div></div>")]
    [InlineData("<div data-bergamot-json-root=\"1\"><div data-bergamot-json-id=\"0\">one</div><div data-bergamot-json-id=\"2\">two</div></div>")]
    [InlineData("<div><div data-bergamot-json-id=\"0\">one</div><div data-bergamot-json-id=\"1\">two</div></div>")]
    [InlineData("<div data-bergamot-json-root=\"1\"><span data-bergamot-json-id=\"0\">one</span><div data-bergamot-json-id=\"1\">two</div></div>")]
    [InlineData("<div data-bergamot-json-root=\"1\">lost<div data-bergamot-json-id=\"0\">one</div><div data-bergamot-json-id=\"1\">two</div></div>")]
    public void FromHtml_RejectsUnrestorableStructure(string html)
    {
        var context = JsonHtmlTranslationConverter.Instance.ToHtml("[\"one\",\"two\"]");
        Assert.Throws<InvalidDataException>(() => context.FromHtml(html));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("{\"value\":}")]
    public void ToHtml_RejectsInvalidJson(string json)
        => Assert.ThrowsAny<JsonException>(() => JsonHtmlTranslationConverter.Instance.ToHtml(json));
}
