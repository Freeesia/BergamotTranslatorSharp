using REDox.Json;

namespace BergamotTranslatorSharp.REDox;

/// <summary>A JSON5 document with its nonblank string values selected for translation.</summary>
public sealed class Json5TranslationDocument : ITranslatableDocument<string>
{
    private readonly RedoxTranslationDocument<string, string> document;

    public IReadOnlyList<string> Values => document.Values;

    public Json5TranslationDocument(string json5)
    {
        ArgumentNullException.ThrowIfNull(json5);
        document = new RedoxTranslationDocument<string, string>(
            json5,
            static source => Json5Document.Parse(source, options: new Json5DocumentOptions { PreserveTrivia = true }),
            static root => Json5Document.EncodeToString(root, new Json5WriteOptions
            {
                PreserveTrivia = true,
                StringStyle = Json5QuoteStyle.PreserveOrSingle,
            }));
    }

    public string Restore(IReadOnlyList<string> translations) => document.Restore(translations);
}
