using REDox.Ini;

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
        ArgumentNullException.ThrowIfNull(ini);
        var document = new RedoxTranslationDocument<string, string>(
            ini,
            static source => IniDocument.Parse(source, options: new IniDocumentOptions { PreserveTrivia = true }),
            static root => IniDocument.EncodeToString(root, new IniWriteOptions { PreserveTrivia = true }));
        return service.Translate(document, dictionary);
    }
}
