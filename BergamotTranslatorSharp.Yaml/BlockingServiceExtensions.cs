namespace BergamotTranslatorSharp.Yaml;

public static class BlockingServiceExtensions
{
    /// <summary>Translates nonempty YAML string values while preserving YAML structure.</summary>
    public static string TranslateYaml(
        this BlockingService service,
        string yaml,
        IReadOnlyDictionary<string, string>? dictionary = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Translate(new YamlTranslationDocument(yaml), dictionary);
    }
}
