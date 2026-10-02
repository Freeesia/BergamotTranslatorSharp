namespace BergamotTranslatorSharp.REDox.Ini;

public static class BlockingServiceExtensions
{
    /// <summary>Translates nonblank INI string values.</summary>
    public static string TranslateIni(
        this BlockingService service,
        string ini,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new IniTranslationDocument(ini), dictionary);
    }
}
