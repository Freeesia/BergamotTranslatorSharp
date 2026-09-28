namespace BergamotTranslatorSharp;

public sealed partial class BlockingService
{
    /// <summary>Translates the string values of a structured document.</summary>
    public T Translate<T>(
        ITranslatableDocument<T> document,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(BlockingService));

        ArgumentNullException.ThrowIfNull(document);
        var translations = dictionary is null
            ? Translate(document.Values)
            : Translate(document.Values, dictionary);
        return document.Restore(translations);
    }
}
