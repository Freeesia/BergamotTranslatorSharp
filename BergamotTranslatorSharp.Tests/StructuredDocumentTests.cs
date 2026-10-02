using System.Text.Json;
using System.Text.Json.Nodes;
using BergamotTranslatorSharp.Json;
using BergamotTranslatorSharp.REDox;
using BergamotTranslatorSharp.Toml;
using BergamotTranslatorSharp.Yaml;
using REDox;
using REDox.Cbor;
using REDox.Json;
using REDox.MessagePack;
using Tomlyn.Parsing;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace BergamotTranslatorSharp.Tests;

public sealed class StructuredDocumentTests
{
    [Fact]
    public void Json5_RestoresOnlyNonblankStringValuesAndPreservesTrivia()
    {
        const string json5 = """
            {
              // heading
              title: 'Hello',
              HelloKey: '',
              nested: { key: 'World', count: 42 },
              empty: '',
              whitespace: '  ',
            }
            """;
        var document = new Json5TranslationDocument(json5);

        Assert.Equal(["Hello", "World"], document.Values);
        var result = document.Restore(["Bonjour", "Monde"]);
        Assert.Contains("// heading", result);
        Assert.Contains("title: 'Bonjour'", result);
        Assert.Contains("HelloKey: ''", result);
        Assert.Contains("key: 'Monde'", result);
        Assert.Contains("count: 42", result);
        Assert.Contains("empty: ''", result);
        Assert.Contains("whitespace: '  '", result);
        Assert.Throws<InvalidDataException>(() => document.Restore(["only one"]));
    }

    [Fact]
    public void Ini_TranslatesValuesButNotKeys()
    {
        var document = new IniTranslationDocument("# heading\n[app]\ntitle=Hello\ncount=42\n");

        Assert.Equal(["Hello", "42"], document.Values);
        var result = document.Restore(["Bonjour", "42"]);
        Assert.Contains("# heading", result);
        Assert.Contains("title", result);
        Assert.Contains("Bonjour", result);
        Assert.Contains("count", result);
        Assert.Throws<InvalidDataException>(() => document.Restore(["only one"]));
    }

    [Fact]
    public void Cbor_TranslatesOnlyStringValues()
    {
        var source = CborDocument.Encode(new DObject
        {
            ["Not translated"] = "Still not translated",
            ["key"] = "Hello",
            ["count"] = 42,
            ["empty"] = "",
            ["whitespace"] = "  ",
            ["null"] = DValue.Create((object?)null),
            ["binary"] = DValue.Create(new byte[] { 1, 2 }),
            ["array"] = new DArray { "World", true },
        });
        var document = new CborTranslationDocument(source);

        Assert.Equal(["Still not translated", "Hello", "World"], document.Values);
        using var result = CborDocument.Parse(document.Restore(["Still not translated", "Bonjour", "Monde"]));
        Assert.Equal("Bonjour", result.RootElement.GetProperty("key").GetString());
        Assert.Equal(42, result.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("", result.RootElement.GetProperty("empty").GetString());
        Assert.Equal("  ", result.RootElement.GetProperty("whitespace").GetString());
        Assert.Null(result.RootElement.GetProperty("null").GetString());
        Assert.Equal(new byte[] { 1, 2 }, result.RootElement.GetProperty("binary").GetByteString().ToArray());
        Assert.Equal("Monde", result.RootElement.GetProperty("array").AsArray()[0].AsElement().GetString());
        Assert.Throws<InvalidDataException>(() => document.Restore(["only one"]));
    }

    [Fact]
    public void MessagePack_TranslatesOnlyStringValues()
    {
        var source = MessagePackDocument.Encode(new DObject
        {
            ["Not translated"] = "Still not translated",
            ["key"] = "Hello",
            ["count"] = 42,
            ["empty"] = "",
            ["whitespace"] = "  ",
            ["null"] = DValue.Create((object?)null),
            ["binary"] = DValue.Create(new byte[] { 1, 2 }),
            ["array"] = new DArray { "World", true },
        });
        var document = new MessagePackTranslationDocument(source);

        Assert.Equal(["Still not translated", "Hello", "World"], document.Values);
        using var result = MessagePackDocument.Parse(document.Restore(["Still not translated", "Bonjour", "Monde"]));
        Assert.Equal("Bonjour", result.RootElement.GetProperty("key").GetString());
        Assert.Equal(42, result.RootElement.GetProperty("count").GetInt32());
        Assert.Equal("", result.RootElement.GetProperty("empty").GetString());
        Assert.Equal("  ", result.RootElement.GetProperty("whitespace").GetString());
        Assert.Null(result.RootElement.GetProperty("null").GetString());
        Assert.Equal(new byte[] { 1, 2 }, result.RootElement.GetProperty("binary").GetByteString().ToArray());
        Assert.Equal("Monde", result.RootElement.GetProperty("array").AsArray()[0].AsElement().GetString());
        Assert.Throws<InvalidDataException>(() => document.Restore(["only one"]));
    }

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
