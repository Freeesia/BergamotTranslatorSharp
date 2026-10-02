using REDox.Json;

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
        ArgumentNullException.ThrowIfNull(json5);
        var document = new RedoxTranslationDocument<string, string>(
            json5,
            static source => Json5Document.Parse(source, options: new Json5DocumentOptions { PreserveTrivia = true }),
            static root => Json5Document.EncodeToString(root, new Json5WriteOptions
            {
                PreserveTrivia = true,
                StringStyle = Json5QuoteStyle.PreserveOrSingle,
            }));
        return service.Translate(document, dictionary);
    }
}
