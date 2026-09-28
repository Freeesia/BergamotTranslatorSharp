namespace BergamotTranslatorSharp.Toml;

public static class BlockingServiceExtensions
{
    /// <summary>Translates nonempty TOML string values while preserving TOML structure.</summary>
    public static string TranslateToml(
        this BlockingService service,
        string toml,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new TomlTranslationDocument(toml), dictionary);
    }
}
