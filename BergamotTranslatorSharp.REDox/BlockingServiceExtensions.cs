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

    /// <summary>Translates nonblank JSON5 string values while preserving JSON5 trivia.</summary>
    public static string TranslateJson5(
        this BlockingService service,
        string json5,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new Json5TranslationDocument(json5), dictionary);
    }

    /// <summary>Translates nonblank INI string values.</summary>
    public static string TranslateIni(
        this BlockingService service,
        string ini,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new IniTranslationDocument(ini), dictionary);
    }

    /// <summary>Translates nonblank CBOR string values.</summary>
    public static byte[] TranslateCbor(
        this BlockingService service,
        byte[] cbor,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new CborTranslationDocument(cbor), dictionary);
    }

    /// <summary>Translates nonblank MessagePack string values.</summary>
    public static byte[] TranslateMessagePack(
        this BlockingService service,
        byte[] messagePack,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new MessagePackTranslationDocument(messagePack), dictionary);
    }
}
