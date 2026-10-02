namespace BergamotTranslatorSharp.REDox.MessagePack;

public static class BlockingServiceExtensions
{
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
