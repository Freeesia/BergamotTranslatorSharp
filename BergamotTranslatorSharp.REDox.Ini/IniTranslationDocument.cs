using REDox.Ini;

namespace BergamotTranslatorSharp.REDox.Ini;

/// <summary>An INI document with its nonblank string values selected for translation.</summary>
public sealed class IniTranslationDocument : ITranslatableDocument<string>
{
    private readonly RedoxTranslationDocument<string, string> document;

    public IReadOnlyList<string> Values => document.Values;

    public IniTranslationDocument(string ini)
    {
        ArgumentNullException.ThrowIfNull(ini);
        document = new RedoxTranslationDocument<string, string>(
            ini,
            static source => IniDocument.Parse(source, options: new IniDocumentOptions { PreserveTrivia = true }),
            static root => IniDocument.EncodeToString(root, new IniWriteOptions { PreserveTrivia = true }));
    }

    public string Restore(IReadOnlyList<string> translations) => document.Restore(translations);
}
