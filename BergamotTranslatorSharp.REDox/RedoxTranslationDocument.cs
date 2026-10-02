using REDox;

namespace BergamotTranslatorSharp.REDox;

/// <summary>Adapts a REDox parser and encoder to the structured-document translation pipeline.</summary>
public sealed class RedoxTranslationDocument<TInput, TOutput> : ITranslatableDocument<TOutput>
{
    private readonly TInput input;
    private readonly Func<TInput, Document> parse;
    private readonly Func<DElement, TOutput> encode;

    /// <summary>Creates an adapter over the input and its REDox parse and encode operations.</summary>
    public RedoxTranslationDocument(
        TInput input,
        Func<TInput, Document> parse,
        Func<DElement, TOutput> encode)
    {
        ArgumentNullException.ThrowIfNull(parse);
        ArgumentNullException.ThrowIfNull(encode);
        this.input = input;
        this.parse = parse;
        this.encode = encode;
        Values = ReadValues(input, parse);
    }

    public IReadOnlyList<string> Values { get; }

    public TOutput Restore(IReadOnlyList<string> translations)
    {
        ArgumentNullException.ThrowIfNull(translations);
        if (translations.Count != Values.Count)
            throw new InvalidDataException("Translated REDox value count changed.");

        using var document = parse(input);
        var targets = new List<DElement>();
        Collect(document.RootElement, targets);
        if (targets.Count != translations.Count)
            throw new InvalidDataException("Translated REDox value count changed.");

        for (var index = 0; index < targets.Count; index++)
            targets[index].AsValue().ReplaceWith(translations[index]
                ?? throw new InvalidDataException("Translated REDox value is null."));

        return encode(document.RootElement);
    }

    private static IReadOnlyList<string> ReadValues(TInput input, Func<TInput, Document> parse)
    {
        using var document = parse(input);
        return GetValues(document.RootElement);
    }

    internal static IReadOnlyList<string> GetValues(DElement element)
    {
        var targets = GetTargets(element);
        return targets.Select(static target => target.GetString()!).ToArray();
    }

    internal static List<DElement> GetTargets(DElement element)
    {
        var values = new List<DElement>();
        Collect(element, values);
        return values;
    }

    private static void Collect(DElement element, List<DElement> values)
    {
        switch (element.Token.Type)
        {
            case DTokenType.Array:
                foreach (var item in element.EnumerateArray())
                    Collect(item, values);
                break;
            case DTokenType.Map:
                foreach (var pair in element.EnumerateMap())
                    Collect(pair.Value, values);
                break;
            default:
                if (element.Token.Kind == DTokenKind.String
                    && !string.IsNullOrWhiteSpace(element.GetString()))
                    values.Add(element);
                break;
        }
    }
}
