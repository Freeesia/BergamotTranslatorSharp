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
static async Task<int> TranslateAsync(
    [Argument] string source,
    [Argument] string target,
    [Argument] string text,
    bool html = false,
    string? dictionary = null,
    CancellationToken cancellationToken = default)
{
    try
    {
        if (html && dictionary is not null)
            throw new ArgumentException("--dictionary cannot be combined with --html.");
        var terms = dictionary is null ? null : TermDictionaryCsv.Load(dictionary);

        var configurations = await new ModelStore()
            .GetConfigurationPathsAsync(source, target, cancellationToken);
        using var service = new BlockingService(configurations);
        Console.WriteLine(terms is null
            ? service.Translate(text, html)
            : service.Translate(text, terms));
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
