namespace BergamotTranslatorSharp.REDox.Json5;

public static class BlockingServiceExtensions
{
    /// <summary>Translates nonblank JSON5 string values while preserving JSON5 trivia.</summary>
    public static string TranslateJson5(
        this BlockingService service,
        string json5,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new Json5TranslationDocument(json5), dictionary);
    }
}
