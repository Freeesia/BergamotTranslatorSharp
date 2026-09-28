namespace BergamotTranslatorSharp.Json;

public static class BlockingServiceExtensions
{
    /// <summary>Translates nonempty JSON string values while preserving JSON structure.</summary>
    public static string TranslateJson(
        this BlockingService service,
        string json,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new JsonTranslationDocument(json), dictionary);
    }
}
