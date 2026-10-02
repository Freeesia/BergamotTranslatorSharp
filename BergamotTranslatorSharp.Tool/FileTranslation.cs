namespace BergamotTranslatorSharp.Tool;

internal enum TranslationFormat
{
    Json,
    Json5,
    Yaml,
    Toml,
    Ini,
    Cbor,
    MessagePack,
}

internal static class FileTranslation
{
    public static TranslationFormat ResolveFormat(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".json" => TranslationFormat.Json,
            ".json5" => TranslationFormat.Json5,
            ".yaml" or ".yml" => TranslationFormat.Yaml,
            ".toml" => TranslationFormat.Toml,
            ".ini" => TranslationFormat.Ini,
            ".cbor" => TranslationFormat.Cbor,
            ".msgpack" or ".mpk" => TranslationFormat.MessagePack,
            _ => throw new ArgumentException(
                $"Cannot determine the format from file extension '{extension}'. Specify --format.",
                nameof(path)),
        };
    }
}
