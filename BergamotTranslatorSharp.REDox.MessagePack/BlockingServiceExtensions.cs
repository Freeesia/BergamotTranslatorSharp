using REDox.MessagePack;

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
        ArgumentNullException.ThrowIfNull(messagePack);
        var document = new RedoxTranslationDocument<byte[], byte[]>(
            messagePack.ToArray(),
            static source => MessagePackDocument.Parse(source),
            static root => MessagePackDocument.Encode(root));
        return service.Translate(document, dictionary);
    }
}
