using REDox.Cbor;

namespace BergamotTranslatorSharp.REDox;

/// <summary>A CBOR document with its nonblank string values selected for translation.</summary>
public sealed class CborTranslationDocument : ITranslatableDocument<byte[]>
{
    private readonly RedoxTranslationDocument<byte[], byte[]> document;

    public IReadOnlyList<string> Values => document.Values;

    public CborTranslationDocument(byte[] cbor)
    {
        ArgumentNullException.ThrowIfNull(cbor);
        document = new RedoxTranslationDocument<byte[], byte[]>(
            cbor.ToArray(),
            static source => CborDocument.Parse(source),
            static root => CborDocument.Encode(root));
    }

    public byte[] Restore(IReadOnlyList<string> translations) => document.Restore(translations);
}
