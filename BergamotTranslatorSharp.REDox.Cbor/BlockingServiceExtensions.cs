using REDox.Cbor;

namespace BergamotTranslatorSharp.REDox.Cbor;

public static class BlockingServiceExtensions
{
    /// <summary>Translates nonblank CBOR string values.</summary>
    public static byte[] TranslateCbor(
        this BlockingService service,
        byte[] cbor,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(cbor);
        var document = new RedoxTranslationDocument<byte[], byte[]>(
            cbor.ToArray(),
            static source => CborDocument.Parse(source),
            static root => CborDocument.Encode(root));
        return service.Translate(document, dictionary);
    }
}
