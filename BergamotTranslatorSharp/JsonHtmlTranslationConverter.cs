using System.Globalization;
using System.Text.Json.Nodes;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace BergamotTranslatorSharp;

/// <summary>Converts JSON string values to HTML for translation.</summary>
public sealed class JsonHtmlTranslationConverter : IHtmlTranslationConverter<string>
{
    private const string RootAttribute = "data-bergamot-json-root";
    private const string IdAttribute = "data-bergamot-json-id";

    public static JsonHtmlTranslationConverter Instance { get; } = new();

    public IHtmlTranslationContext<string> ToHtml(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var original = JsonNode.Parse(json);
        var document = new HtmlParser().ParseDocument(string.Empty);
        var root = document.CreateElement("div");
        root.SetAttribute(RootAttribute, "1");
        var targets = new List<Target>();
        var path = new List<PathStep>();

        Collect(original);
        return new Context(original, root.OuterHtml, targets);

        void Collect(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject obj:
                    foreach (var property in obj)
                    {
                        path.Add(new PathStep(property.Key, 0));
                        Collect(property.Value);
                        path.RemoveAt(path.Count - 1);
                    }
                    break;

                case JsonArray array:
                    for (var index = 0; index < array.Count; index++)
                    {
                        path.Add(new PathStep(null, index));
                        Collect(array[index]);
                        path.RemoveAt(path.Count - 1);
                    }
                    break;

                case JsonValue value when value.TryGetValue<string>(out var text)
                    && !string.IsNullOrWhiteSpace(text):
                    var entry = document.CreateElement("div");
                    entry.SetAttribute(IdAttribute, targets.Count.ToString(CultureInfo.InvariantCulture));
                    entry.InnerHtml = text;
                    var containsHtml = entry.ChildNodes.Any(static child => child.NodeType is NodeType.Element or NodeType.Comment);
                    if (!containsHtml)
                        entry.TextContent = text;

                    root.AppendChild(entry);
                    targets.Add(new Target(path.ToArray(), containsHtml));
                    break;
            }
        }
    }

    private readonly record struct PathStep(string? PropertyName, int Index);

    private readonly record struct Target(PathStep[] Path, bool ContainsHtml);

    private sealed class Context(JsonNode? original, string html, List<Target> targets) : IHtmlTranslationContext<string>
    {
        public string Html { get; } = html;

        public bool HasTranslatableContent => targets.Count != 0;

        public string FromHtml(string translatedHtml)
        {
            ArgumentNullException.ThrowIfNull(translatedHtml);

            var document = new HtmlParser().ParseDocument(translatedHtml);
            var body = document.Body ?? throw new InvalidDataException("Translated HTML has no body.");
            var rootNodes = body.ChildNodes.Where(static node => !IsWhitespace(node)).ToArray();
            if (rootNodes.Length != 1 || rootNodes[0] is not IElement root
                || root.LocalName != "div" || root.GetAttribute(RootAttribute) != "1")
                throw new InvalidDataException("Translated HTML has no unique JSON root element.");

            var entries = new IElement[targets.Count];
            var seen = new bool[targets.Count];
            foreach (var child in root.ChildNodes)
            {
                if (IsWhitespace(child))
                    continue;

                if (child is not IElement entry || entry.LocalName != "div")
                    throw new InvalidDataException("Translated HTML contains an invalid JSON entry.");

                var id = entry.GetAttribute(IdAttribute);
                if (!int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                    || index < 0 || index >= targets.Count
                    || id != index.ToString(CultureInfo.InvariantCulture))
                    throw new InvalidDataException("Translated HTML contains an unknown JSON entry identifier.");

                if (seen[index])
                    throw new InvalidDataException($"Translated HTML contains duplicate JSON entry identifier {id}.");

                seen[index] = true;
                entries[index] = entry;
            }

            if (seen.Any(static present => !present))
                throw new InvalidDataException("Translated HTML is missing a JSON entry identifier.");

            var result = original?.DeepClone();
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                var translatedValue = target.ContainsHtml ? entries[index].InnerHtml : entries[index].TextContent;
                result = Replace(result, target.Path, translatedValue);
            }

            return result?.ToJsonString() ?? "null";
        }

        private static bool IsWhitespace(INode node)
            => node.NodeType == NodeType.Text && string.IsNullOrWhiteSpace(node.TextContent);

        private static JsonNode? Replace(JsonNode? root, PathStep[] path, string translation)
        {
            if (path.Length == 0)
                return JsonValue.Create(translation);

            var parent = root;
            for (var index = 0; index < path.Length - 1; index++)
            {
                var step = path[index];
                parent = step.PropertyName is { } name
                    ? (parent as JsonObject)?[name]
                    : (parent as JsonArray)?[step.Index];
            }

            var last = path[^1];
            if (last.PropertyName is { } propertyName && parent is JsonObject obj
                && obj[propertyName] is JsonValue)
                obj[propertyName] = translation;
            else if (last.PropertyName is null && parent is JsonArray array
                && array[last.Index] is JsonValue)
                array[last.Index] = translation;
            else
                throw new InvalidDataException("The original JSON entry cannot be restored.");

            return root;
        }
    }
}
