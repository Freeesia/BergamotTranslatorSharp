using System.Text.Json.Nodes;
using BergamotTranslatorSharp.Json;
using BergamotTranslatorSharp.Toml;
using BergamotTranslatorSharp.Yaml;
using Xunit;

namespace BergamotTranslatorSharp.Tests;

[Collection(nameof(TranslatorModelCollection))]
public sealed class StructuredTranslationTests(TranslatorModelFixture models)
{
    private static readonly Dictionary<string, string> Dictionary = new()
    {
        ["world"] = "指定訳",
    };

    [Fact]
    public void TranslateJson_AppliesDictionaryToValues()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));
        var result = JsonNode.Parse(service.TranslateJson(
            """{"message":"Hello world!","count":42,"empty":""}""", Dictionary));

        Assert.Contains("指定訳", result?["message"]?.GetValue<string>());
        Assert.Equal(42, result?["count"]?.GetValue<int>());
        Assert.Equal("", result?["empty"]?.GetValue<string>());

        var withoutDictionary = JsonNode.Parse(service.TranslateJson("""{"message":"Hello world!"}"""));
        Assert.Equal(service.Translate("Hello world!"), withoutDictionary?["message"]?.GetValue<string>());
    }

    [Fact]
    public void TranslateYaml_AppliesDictionaryToValues()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));
        var result = service.TranslateYaml("# comment\nmessage: Hello world!\ncount: 42\n", Dictionary);

        Assert.Contains("指定訳", result);
        Assert.Contains("# comment", result);
        Assert.Contains("count: 42", result);
    }

    [Fact]
    public void TranslateToml_AppliesDictionaryToValues()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));
        var result = service.TranslateToml("# comment\nmessage = 'Hello world!'\ncount = 42\n", Dictionary);

        Assert.Contains("指定訳", result);
        Assert.Contains("# comment", result);
        Assert.Contains("count = 42", result);
    }
}
