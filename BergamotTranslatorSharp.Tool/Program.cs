using BergamotTranslatorSharp;
using BergamotTranslatorSharp.Json;
using BergamotTranslatorSharp.REDox;
using BergamotTranslatorSharp.Toml;
using BergamotTranslatorSharp.Tool;
using BergamotTranslatorSharp.Yaml;
using ConsoleAppFramework;
using REDox.Cbor;
using REDox.Ini;
using REDox.Json;
using REDox.MessagePack;

ToolNativeLibrary.Configure();
await ConsoleApp.RunAsync(args, TranslateAsync);

/// <summary>Translate text with a Mozilla model downloaded on demand.</summary>
/// <param name="source">Source language code, such as en.</param>
/// <param name="target">Target language code, such as ja.</param>
/// <param name="text">Text to translate.</param>
/// <param name="html">Preserve HTML markup.</param>
/// <param name="dictionary">Path to a two-column terminology CSV file.</param>
/// <param name="file">Path to a structured document to translate.</param>
/// <param name="format">Structured document format. Defaults to the file extension.</param>
/// <param name="output">Path to write the translated document to.</param>
static async Task<int> TranslateAsync(
    [Argument] string source,
    [Argument] string target,
    [Argument] string? text = null,
    bool html = false,
    string? dictionary = null,
    string? file = null,
    TranslationFormat? format = null,
    string? output = null,
    CancellationToken cancellationToken = default)
{
    try
    {
        if (file is null)
        {
            if (text is null)
                throw new ArgumentException("Specify text or --file.");
            if (format is not null || output is not null)
                throw new ArgumentException("--format and --output require --file.");
            if (html && dictionary is not null)
                throw new ArgumentException("--dictionary cannot be combined with --html.");
        }
        else
        {
            if (text is not null)
                throw new ArgumentException("Text and --file cannot be specified together.");
            if (html)
                throw new ArgumentException("--html cannot be combined with --file.");
        }

        var resolvedFormat = file is null
            ? format
            : format ?? FileTranslation.ResolveFormat(file);
        var terms = dictionary is null ? null : TermDictionaryCsv.Load(dictionary);

        var configurations = await new ModelStore()
            .GetConfigurationPathsAsync(source, target, cancellationToken);
        using var service = new BlockingService(configurations);
        if (file is null)
        {
            Console.WriteLine(terms is null
                ? service.Translate(text!, html)
                : service.Translate(text!, terms));
        }
        else
        {
            if (resolvedFormat is TranslationFormat.Cbor or TranslationFormat.MessagePack)
            {
                var input = await File.ReadAllBytesAsync(file, cancellationToken);
                var translated = resolvedFormat switch
                {
                    TranslationFormat.Cbor => service.Translate(new RedoxTranslationDocument<byte[], byte[]>(
                        input, static source => CborDocument.Parse(source),
                        static root => CborDocument.Encode(root)), terms),
                    TranslationFormat.MessagePack => service.Translate(new RedoxTranslationDocument<byte[], byte[]>(
                        input, static source => MessagePackDocument.Parse(source),
                        static root => MessagePackDocument.Encode(root)), terms),
                    _ => throw new ArgumentOutOfRangeException(nameof(format)),
                };
                if (output is null)
                    await Console.OpenStandardOutput().WriteAsync(translated, cancellationToken);
                else
                    await File.WriteAllBytesAsync(output, translated, cancellationToken);
            }
            else
            {
                var input = await File.ReadAllTextAsync(file, cancellationToken);
                var translated = resolvedFormat switch
                {
                    TranslationFormat.Json => service.Translate(new JsonTranslationDocument(input), terms),
                    TranslationFormat.Json5 => service.Translate(new RedoxTranslationDocument<string, string>(
                        input,
                        static source => Json5Document.Parse(source,
                            options: new Json5DocumentOptions { PreserveTrivia = true }),
                        static root => Json5Document.EncodeToString(root, new Json5WriteOptions
                        {
                            PreserveTrivia = true,
                            StringStyle = Json5QuoteStyle.PreserveOrSingle,
                        })), terms),
                    TranslationFormat.Yaml => service.Translate(new YamlTranslationDocument(input), terms),
                    TranslationFormat.Toml => service.Translate(new TomlTranslationDocument(input), terms),
                    TranslationFormat.Ini => service.Translate(new RedoxTranslationDocument<string, string>(
                        input,
                        static source => IniDocument.Parse(source,
                            options: new IniDocumentOptions { PreserveTrivia = true }),
                        static root => IniDocument.EncodeToString(root,
                            new IniWriteOptions { PreserveTrivia = true })), terms),
                    _ => throw new ArgumentOutOfRangeException(nameof(format)),
                };
                if (output is null)
                    Console.Write(translated);
                else
                    await File.WriteAllTextAsync(output, translated, cancellationToken);
            }
        }
        return 0;
    }
    catch (Exception exception) when (exception is ArgumentException or FileNotFoundException or
        InvalidDataException or HttpRequestException or IOException or InvalidOperationException or
        UnauthorizedAccessException or DllNotFoundException or BadImageFormatException or
        EntryPointNotFoundException or PlatformNotSupportedException)
    {
        Console.Error.WriteLine(exception.Message);
        return 1;
    }
}
