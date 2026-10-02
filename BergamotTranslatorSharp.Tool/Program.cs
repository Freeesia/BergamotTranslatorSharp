using BergamotTranslatorSharp;
using BergamotTranslatorSharp.Tool;
using ConsoleAppFramework;

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
    string? format = null,
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

        TranslationFormat? resolvedFormat = file is null ? null : FileTranslation.ResolveFormat(file, format);
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
            var translated = FileTranslation.Translate(service, file, resolvedFormat!.Value, terms);
            if (output is null)
                await Console.OpenStandardOutput().WriteAsync(translated, cancellationToken);
            else
                await File.WriteAllBytesAsync(output, translated, cancellationToken);
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
