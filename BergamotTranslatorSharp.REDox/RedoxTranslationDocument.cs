using REDox;

namespace BergamotTranslatorSharp.REDox;

internal sealed class RedoxTranslationDocument<TInput, TOutput>(
    TInput input,
    Func<TInput, Document> parse,
    Func<DElement, TOutput> encode) : ITranslatableDocument<TOutput>
{
    public IReadOnlyList<string> Values { get; } = ReadValues(input, parse);

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
