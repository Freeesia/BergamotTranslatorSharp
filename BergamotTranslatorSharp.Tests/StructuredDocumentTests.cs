using System.Text.Json;
using System.Text.Json.Nodes;
using BergamotTranslatorSharp.Json;
using BergamotTranslatorSharp.Toml;
using BergamotTranslatorSharp.Yaml;
using Tomlyn.Parsing;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace BergamotTranslatorSharp.Tests;

public sealed class StructuredDocumentTests
{
    [Fact]
    public void Json_RestoresOnlyNonblankStringValues()
    {
        var document = new JsonTranslationDocument(
            """{"title":"Hello","nested":["World",42,true,null,"", "  "],"count":2.5}""");

        Assert.Equal(["Hello", "World"], document.Values);
        var result = JsonNode.Parse(document.Restore(["Bonjour", "Monde"]));
        Assert.Equal("Bonjour", result?["title"]?.GetValue<string>());
        Assert.Equal("Monde", result?["nested"]?[0]?.GetValue<string>());
        Assert.Equal(42, result?["nested"]?[1]?.GetValue<int>());
        Assert.Equal(true, result?["nested"]?[2]?.GetValue<bool>());
        Assert.Null(result?["nested"]?[3]);
        Assert.Equal("", result?["nested"]?[4]?.GetValue<string>());
        Assert.Equal("  ", result?["nested"]?[5]?.GetValue<string>());
        Assert.Equal(2.5, result?["count"]?.GetValue<double>());
        Assert.Null(result?["Bonjour"]);

        Assert.Equal("\"Bonjour\"", new JsonTranslationDocument("\"Hello\"").Restore(["Bonjour"]));
        Assert.ThrowsAny<JsonException>(() => new JsonTranslationDocument("{"));
        Assert.Throws<InvalidDataException>(() => document.Restore(["only one"]));
    }

    [Fact]
    public void Yaml_RestoresValuesWithoutChangingKeysAnchorsOrComments()
    {
        const string yaml = """
            # heading
            title: &greeting Hello # inline
            copy: *greeting
            items: [World, 42, true, null, '', '   ']
            quoted: !!str true
            block: |
              Good
              morning
            """;
        var document = new YamlTranslationDocument(yaml);

        Assert.Equal(4, document.Values.Count);
        var result = document.Restore(document.Values.Select(static value => value
            .Replace("Hello", "Bonjour")
            .Replace("World", "Monde")
            .Replace("true", "vrai")
            .Replace("Good\nmorning", "Bon\nmatin")).ToArray());

        Assert.Contains("# heading", result);
        Assert.Contains("# inline", result);
        Assert.Contains("&greeting \"Bonjour\"", result);
        Assert.Contains("copy: *greeting", result);
        Assert.Contains("[\"Monde\", 42, true, null, '', '   ']", result);
        Assert.Contains("!!str \"vrai\"", result);
        var stream = new YamlStream();
        stream.Load(new StringReader(result));
        var mapping = Assert.IsType<YamlMappingNode>(stream.Documents[0].RootNode);
        Assert.Equal("Bonjour", ((YamlScalarNode)mapping.Children[new YamlScalarNode("copy")]).Value);

        Assert.Empty(new YamlTranslationDocument("&label Hello: 1\ncopy: *label\n").Values);
        Assert.ThrowsAny<Exception>(() => new YamlTranslationDocument("value: [unfinished"));
    }

    [Fact]
    public void Toml_RestoresValuesWithoutChangingKeysTypesOrComments()
    {
        const string toml = "# heading\r\n'quoted key' = 'Hello' # inline\r\nnumber = 42\r\n"
            + "enabled = true\nitems = [\"World\", 3.5, \"\", \"   \"]\n"
            + "[section]\nmessage = \"\"\"Good\nmorning\"\"\"\n";
        var document = new TomlTranslationDocument(toml);

        Assert.Equal(3, document.Values.Count);
        var result = document.Restore(document.Values.Select(static value => value
            .Replace("Hello", "Bonjour")
            .Replace("World", "Monde")
            .Replace("Good\nmorning", "Bon\nmatin")).ToArray());

        Assert.Contains("# heading\r\n", result);
        Assert.Contains("'quoted key' = \"Bonjour\" # inline", result);
        Assert.Contains("number = 42", result);
        Assert.Contains("enabled = true", result);
        Assert.Contains("items = [\"Monde\", 3.5, \"\", \"   \"]", result);
        Assert.Contains("message = \"Bon\\nmatin\"", result);
        SyntaxParser.ParseStrict(result);
        Assert.ThrowsAny<Exception>(() => new TomlTranslationDocument("value = ["));
    }
}
