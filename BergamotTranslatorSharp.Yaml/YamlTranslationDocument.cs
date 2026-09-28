using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace BergamotTranslatorSharp.Yaml;

/// <summary>A YAML document with the source locations of its translatable string values.</summary>
public sealed class YamlTranslationDocument : ITranslatableDocument<string>
{
    private readonly string source;
    private readonly Target[] targets;

    public IReadOnlyList<string> Values { get; }

    public YamlTranslationDocument(string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        source = yaml;
        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));

        // Aliases can refer to a mapping key. Protect that node before visiting values.
        var keys = new HashSet<YamlNode>(ReferenceEqualityComparer.Instance);
        var inspected = new HashSet<YamlNode>(ReferenceEqualityComparer.Instance);
        foreach (var document in stream.Documents)
            MarkKeys(document.RootNode, keys, inspected);

        var scalarDeserializer = new DeserializerBuilder()
            .WithAttemptingUnquotedStringTypeDeserialization()
            .Build();
        var found = new List<Target>();
        var visited = new HashSet<YamlNode>(ReferenceEqualityComparer.Instance);
        foreach (var document in stream.Documents)
            Collect(document.RootNode, yaml, keys, visited, scalarDeserializer, found);

        targets = found.OrderBy(static target => target.Start).ToArray();
        Values = Array.AsReadOnly(targets.Select(static target => target.Value).ToArray());
    }

    public string Restore(IReadOnlyList<string> translations)
    {
        ArgumentNullException.ThrowIfNull(translations);
        if (translations.Count != targets.Length)
            throw new InvalidDataException("Translated YAML value count changed.");

        var result = new StringBuilder(source);
        for (var index = targets.Length - 1; index >= 0; index--)
        {
            var target = targets[index];
            var value = translations[index]
                ?? throw new InvalidDataException("Translated YAML value is null.");
            result.Remove(target.Start, target.End - target.Start);
            result.Insert(target.Start, target.Prefix + Quote(value));
        }
        return result.ToString();
    }

    private static void MarkKeys(
        YamlNode node, HashSet<YamlNode> keys, HashSet<YamlNode> inspected)
    {
        if (!inspected.Add(node))
            return;

        switch (node)
        {
            case YamlMappingNode mapping:
                foreach (var pair in mapping.Children)
                {
                    Protect(pair.Key, keys);
                    MarkKeys(pair.Value, keys, inspected);
                }
                break;
            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                    MarkKeys(child, keys, inspected);
                break;
        }
    }

    private static void Protect(YamlNode node, HashSet<YamlNode> keys)
    {
        if (!keys.Add(node))
            return;

        switch (node)
        {
            case YamlMappingNode mapping:
                foreach (var pair in mapping.Children)
                {
                    Protect(pair.Key, keys);
                    Protect(pair.Value, keys);
                }
                break;
            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                    Protect(child, keys);
                break;
        }
    }

    private static void Collect(
        YamlNode node, string source, HashSet<YamlNode> keys,
        HashSet<YamlNode> visited, IDeserializer scalarDeserializer, List<Target> found)
    {
        if (!visited.Add(node) || keys.Contains(node))
            return;

        switch (node)
        {
            case YamlMappingNode mapping:
                foreach (var pair in mapping.Children)
                    Collect(pair.Value, source, keys, visited, scalarDeserializer, found);
                break;
            case YamlSequenceNode sequence:
                foreach (var child in sequence.Children)
                    Collect(child, source, keys, visited, scalarDeserializer, found);
                break;
            case YamlScalarNode scalar when IsStringValue(scalar, scalarDeserializer):
                var start = checked((int)scalar.Start.Index);
                var end = checked((int)scalar.End.Index);
                found.Add(new Target(start, end, GetPrefix(source.AsSpan(start, end - start)), scalar.Value!));
                break;
        }
    }

    private static bool IsStringValue(YamlScalarNode scalar, IDeserializer scalarDeserializer)
    {
        if (string.IsNullOrWhiteSpace(scalar.Value))
            return false;

        var tag = scalar.Tag.ToString();
        if (tag == "tag:yaml.org,2002:str")
            return true;
        if (tag != "?")
            return false;
        if (scalar.Style != ScalarStyle.Plain)
            return true;

        try
        {
            return scalarDeserializer.Deserialize<object>(scalar.Value!) is string;
        }
        catch (YamlException)
        {
            return true;
        }
    }

    private static string GetPrefix(ReadOnlySpan<char> source)
    {
        var index = 0;
        while (index < source.Length && (source[index] == '&' || source[index] == '!'))
        {
            while (index < source.Length && !char.IsWhiteSpace(source[index]))
                index++;
            while (index < source.Length && char.IsWhiteSpace(source[index]))
                index++;
        }
        return source[..index].ToString();
    }

    private static string Quote(string value)
    {
        var result = new StringBuilder(value.Length + 2).Append('"');
        foreach (var character in value)
        {
            switch (character)
            {
                case '"': result.Append("\\\""); break;
                case '\\': result.Append("\\\\"); break;
                case '\n': result.Append("\\n"); break;
                case '\r': result.Append("\\r"); break;
                case '\t': result.Append("\\t"); break;
                default:
                    if (character < ' ' || character == '\x7F')
                        result.Append("\\u").Append(((int)character).ToString("X4"));
                    else
                        result.Append(character);
                    break;
            }
        }
        return result.Append('"').ToString();
    }

    private readonly record struct Target(int Start, int End, string Prefix, string Value);
}
