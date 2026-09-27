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
static async Task<int> TranslateAsync(
    [Argument] string source,
    [Argument] string target,
    [Argument] string text,
    bool html = false,
    CancellationToken cancellationToken = default)
{
    try
    {
        var configurations = await new ModelStore()
            .GetConfigurationPathsAsync(source, target, cancellationToken);
        using var service = new BlockingService(configurations);
        Console.WriteLine(service.Translate(text, html));
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
