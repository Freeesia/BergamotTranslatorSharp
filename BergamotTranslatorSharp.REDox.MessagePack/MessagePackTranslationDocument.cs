using REDox.MessagePack;

namespace BergamotTranslatorSharp.REDox.MessagePack;

/// <summary>A MessagePack document with its nonblank string values selected for translation.</summary>
public sealed class MessagePackTranslationDocument : ITranslatableDocument<byte[]>
{
    private readonly RedoxTranslationDocument<byte[], byte[]> document;

    public IReadOnlyList<string> Values => document.Values;

    public MessagePackTranslationDocument(byte[] messagePack)
    {
        ArgumentNullException.ThrowIfNull(messagePack);
        document = new RedoxTranslationDocument<byte[], byte[]>(
            messagePack.ToArray(),
            static source => MessagePackDocument.Parse(source),
            static root => MessagePackDocument.Encode(root));
    }

    public byte[] Restore(IReadOnlyList<string> translations) => document.Restore(translations);
}
