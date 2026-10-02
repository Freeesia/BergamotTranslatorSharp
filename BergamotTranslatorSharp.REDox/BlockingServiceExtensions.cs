using REDox;

namespace BergamotTranslatorSharp.REDox;

public static class BlockingServiceExtensions
{
    /// <summary>Translates nonblank string values in a clone of a REDox element.</summary>
    public static DElement TranslateRedox(
        this BlockingService service,
        DElement element,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new RedoxElementTranslationDocument(element), dictionary);
    }

}
