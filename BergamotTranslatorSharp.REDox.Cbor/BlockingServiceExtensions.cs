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
        return service.Translate(new CborTranslationDocument(cbor), dictionary);
    }
}
