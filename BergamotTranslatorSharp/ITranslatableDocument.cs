namespace BergamotTranslatorSharp;

/// <summary>A document whose string values can be translated and restored to its original structure.</summary>
/// <typeparam name="T">The restored document type.</typeparam>
public interface ITranslatableDocument<T>
{
    /// <summary>Nonempty string values to translate, in document order.</summary>
    IReadOnlyList<string> Values { get; }

    /// <summary>Replaces the selected values with their translations.</summary>
    T Restore(IReadOnlyList<string> translations);
}
