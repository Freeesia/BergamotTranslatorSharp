using System.Text.Json.Nodes;

namespace BergamotTranslatorSharp.Json;

/// <summary>A JSON document with its translatable string values selected.</summary>
public sealed class JsonTranslationDocument : ITranslatableDocument<string>
{
    private readonly JsonNode? original;

    public IReadOnlyList<string> Values { get; }

    public JsonTranslationDocument(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        original = JsonNode.Parse(json);
        var values = new List<string>();
        Collect(original, values);
        Values = values.AsReadOnly();
    }

    public string Restore(IReadOnlyList<string> translations)
    {
        ArgumentNullException.ThrowIfNull(translations);
        if (translations.Count != Values.Count)
            throw new InvalidDataException("Translated JSON value count changed.");

        var index = 0;
        return Replace(original?.DeepClone(), translations, ref index)?.ToJsonString() ?? "null";
    }

    private static void Collect(JsonNode? node, List<string> values)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                    Collect(property.Value, values);
                break;
            case JsonArray array:
                foreach (var item in array)
                    Collect(item, values);
                break;
            case JsonValue value when value.TryGetValue<string>(out var text)
                && !string.IsNullOrWhiteSpace(text):
                values.Add(text);
                break;
        }
    }

    private static JsonNode? Replace(JsonNode? node, IReadOnlyList<string> translations, ref int index)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(static property => property.Key).ToArray())
                {
                    var current = obj[key];
                    var replacement = Replace(current, translations, ref index);
                    if (!ReferenceEquals(current, replacement))
                        obj[key] = replacement;
                }
                break;
            case JsonArray array:
                for (var position = 0; position < array.Count; position++)
                {
                    var current = array[position];
                    var replacement = Replace(current, translations, ref index);
                    if (!ReferenceEquals(current, replacement))
                        array[position] = replacement;
                }
                break;
            case JsonValue value when value.TryGetValue<string>(out var text)
                && !string.IsNullOrWhiteSpace(text):
                return JsonValue.Create(translations[index++]
                    ?? throw new InvalidDataException("Translated JSON value is null."));
        }
        return node;
    }
}
