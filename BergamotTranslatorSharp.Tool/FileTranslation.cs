using System.Text;
using BergamotTranslatorSharp.Json;
using BergamotTranslatorSharp.REDox.Cbor;
using BergamotTranslatorSharp.REDox.Ini;
using BergamotTranslatorSharp.REDox.Json5;
using BergamotTranslatorSharp.REDox.MessagePack;
using BergamotTranslatorSharp.Toml;
using BergamotTranslatorSharp.Yaml;

namespace BergamotTranslatorSharp.Tool;

internal enum TranslationFormat
{
    Json,
    Json5,
    Yaml,
    Toml,
    Ini,
    Html,
    Cbor,
    MessagePack,
}

internal static class FileTranslation
{
    public static TranslationFormat ResolveFormat(string path, string? format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (format is not null)
        {
            return format.ToLowerInvariant() switch
            {
                "json" => TranslationFormat.Json,
                "json5" => TranslationFormat.Json5,
                "yaml" or "yml" => TranslationFormat.Yaml,
                "toml" => TranslationFormat.Toml,
                "ini" => TranslationFormat.Ini,
                "html" or "htm" => TranslationFormat.Html,
                "cbor" => TranslationFormat.Cbor,
                "messagepack" or "msgpack" => TranslationFormat.MessagePack,
                _ => throw new ArgumentException($"Unsupported file format '{format}'.", nameof(format)),
            };
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".json" => TranslationFormat.Json,
            ".json5" => TranslationFormat.Json5,
            ".yaml" or ".yml" => TranslationFormat.Yaml,
            ".toml" => TranslationFormat.Toml,
            ".ini" => TranslationFormat.Ini,
            ".html" or ".htm" => TranslationFormat.Html,
            ".cbor" => TranslationFormat.Cbor,
            ".msgpack" or ".mpk" => TranslationFormat.MessagePack,
            _ => throw new ArgumentException(
                $"Cannot determine the format from file extension '{extension}'. Specify --format.",
                nameof(path)),
        };
    }

    public static byte[] Translate(
        BlockingService service,
        string path,
        TranslationFormat format,
        IReadOnlyDictionary<string, string>? dictionary)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (format is TranslationFormat.Cbor or TranslationFormat.MessagePack)
        {
            var bytes = File.ReadAllBytes(path);
            return format switch
            {
                TranslationFormat.Cbor => service.TranslateCbor(bytes, dictionary),
                TranslationFormat.MessagePack => service.TranslateMessagePack(bytes, dictionary),
                _ => throw new ArgumentOutOfRangeException(nameof(format)),
            };
        }

        var text = File.ReadAllText(path);
        if (format == TranslationFormat.Html)
            return Encoding.UTF8.GetBytes(dictionary is null
                ? service.Translate(text, html: true)
                : service.Translate(text, dictionary));

        var translatedText = format switch
        {
            TranslationFormat.Json => service.TranslateJson(text, dictionary),
            TranslationFormat.Json5 => service.TranslateJson5(text, dictionary),
            TranslationFormat.Yaml => service.TranslateYaml(text, dictionary),
            TranslationFormat.Toml => service.TranslateToml(text, dictionary),
            TranslationFormat.Ini => service.TranslateIni(text, dictionary),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
        return Encoding.UTF8.GetBytes(translatedText);
    }
}
