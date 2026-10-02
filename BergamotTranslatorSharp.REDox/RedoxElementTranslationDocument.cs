using REDox;

namespace BergamotTranslatorSharp.REDox;

/// <summary>A cloned REDox element with its nonblank string values selected for translation.</summary>
public sealed class RedoxElementTranslationDocument : ITranslatableDocument<DElement>
{
    private readonly DElement original;

    public IReadOnlyList<string> Values { get; }

    /// <summary>Creates a translatable snapshot without modifying the supplied element.</summary>
    public RedoxElementTranslationDocument(DElement element)
    {
        original = element.Clone();
        Values = RedoxTranslationDocument<string, string>.GetValues(original);
    }

    /// <summary>Returns a translated clone; the original element and this snapshot remain unchanged.</summary>
    public DElement Restore(IReadOnlyList<string> translations)
    {
        ArgumentNullException.ThrowIfNull(translations);
        if (translations.Count != Values.Count)
            throw new InvalidDataException("Translated REDox value count changed.");

        var translated = original.Clone();
        var targets = RedoxTranslationDocument<string, string>.GetTargets(translated);
        if (targets.Count != translations.Count)
            throw new InvalidDataException("Translated REDox value count changed.");

        for (var index = 0; index < targets.Count; index++)
            targets[index].AsValue().ReplaceWith(translations[index]
                ?? throw new InvalidDataException("Translated REDox value is null."));

        return translated;
    }
}
