using System.Text;
using Tomlyn.Parsing;
using Tomlyn.Syntax;

namespace BergamotTranslatorSharp.Toml;

/// <summary>A TOML document with the source locations of its translatable string values.</summary>
public sealed class TomlTranslationDocument : ITranslatableDocument<string>
{
    private readonly string source;
    private readonly Target[] targets;

    public IReadOnlyList<string> Values { get; }

    public TomlTranslationDocument(string toml)
    {
        ArgumentNullException.ThrowIfNull(toml);
        source = toml;
        var document = SyntaxParser.ParseStrict(toml);
        targets = document.Descendants()
            .OfType<StringValueSyntax>()
            .Where(static node => node.Parent is not KeySyntax && !string.IsNullOrWhiteSpace(node.Value))
            .Select(static node => new Target(
                node.Span.Offset, node.Span.Offset + node.Span.Length, node.Value!))
            .OrderBy(static target => target.Start)
            .ToArray();
        Values = Array.AsReadOnly(targets.Select(static target => target.Value).ToArray());
    }

    public string Restore(IReadOnlyList<string> translations)
    {
        ArgumentNullException.ThrowIfNull(translations);
        if (translations.Count != targets.Length)
            throw new InvalidDataException("Translated TOML value count changed.");

        var result = new StringBuilder(source);
        for (var index = targets.Length - 1; index >= 0; index--)
        {
            var target = targets[index];
            var value = translations[index]
                ?? throw new InvalidDataException("Translated TOML value is null.");
            result.Remove(target.Start, target.End - target.Start);
            result.Insert(target.Start, Quote(value));
        }
        return result.ToString();
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

    private readonly record struct Target(int Start, int End, string Value);
}
