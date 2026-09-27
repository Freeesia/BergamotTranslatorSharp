using System.Globalization;
using System.Net;
using System.Text;

namespace BergamotTranslatorSharp;

internal static class DictionaryTranslation
{
    internal readonly record struct PreparedText(string Html, string[] Replacements);

    private readonly record struct Match(int Start, string Key, string Value)
    {
        public int End => Start + Key.Length;
    }

    public static KeyValuePair<string, string>[] Validate(IReadOnlyDictionary<string, string> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        var entries = dictionary.ToArray();
        foreach (var (key, value) in entries)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Dictionary keys must not be empty or null.", nameof(dictionary));
            if (value is null)
                throw new ArgumentException("Dictionary values must not be null.", nameof(dictionary));
        }
        return entries;
    }

    public static PreparedText Prepare(string text, KeyValuePair<string, string>[] entries)
    {
        var candidates = new List<Match>();
        foreach (var (key, value) in entries)
        {
            var start = 0;
            while (start < text.Length)
            {
                var index = text.IndexOf(key, start, StringComparison.Ordinal);
                if (index < 0)
                    break;
                candidates.Add(new Match(index, key, value));
                start = index + 1;
            }
        }

        // Resolve every overlap by key length before assigning IDs in source order.
        candidates.Sort(static (a, b) =>
        {
            var byLength = b.Key.Length.CompareTo(a.Key.Length);
            return byLength != 0 ? byLength : a.Start.CompareTo(b.Start);
        });

        var occupied = new bool[text.Length];
        var selected = new List<Match>();
        foreach (var candidate in candidates)
        {
            var overlaps = false;
            for (var i = candidate.Start; i < candidate.End; i++)
            {
                if (occupied[i])
                {
                    overlaps = true;
                    break;
                }
            }
            if (overlaps)
                continue;

            selected.Add(candidate);
            for (var i = candidate.Start; i < candidate.End; i++)
                occupied[i] = true;
        }
        selected.Sort(static (a, b) => a.Start.CompareTo(b.Start));

        var html = new StringBuilder(text.Length + selected.Count * 35);
        var replacements = new string[selected.Count];
        var position = 0;
        for (var id = 0; id < selected.Count; id++)
        {
            var match = selected[id];
            AppendEscaped(html, text.AsSpan(position, match.Start - position));
            html.Append("<bts-term data-id=\"").Append(id).Append("\">");
            AppendEscaped(html, text.AsSpan(match.Start, match.Key.Length));
            html.Append("</bts-term>");
            replacements[id] = match.Value;
            position = match.End;
        }
        AppendEscaped(html, text.AsSpan(position));
        return new PreparedText(html.ToString(), replacements);
    }

    private static void AppendEscaped(StringBuilder output, ReadOnlySpan<char> text)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var entity = text[i] switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                _ => null,
            };
            if (entity is null)
                continue;

            output.Append(text[start..i]);
            output.Append(entity);
            start = i + 1;
        }
        output.Append(text[start..]);
    }

    public static string Restore(string html, IReadOnlyList<string> replacements)
    {
        var output = new StringBuilder(html.Length);
        var emitted = new bool[replacements.Count];
        var termDepth = 0;
        var position = 0;

        while (position < html.Length)
        {
            var tagStart = html.IndexOf('<', position);
            if (tagStart < 0)
            {
                AppendText(html.AsSpan(position));
                break;
            }

            AppendText(html.AsSpan(position, tagStart - position));
            if (!TryReadTag(html, tagStart, out var tagEnd, out var name, out var attributes, out var closing, out var selfClosing))
            {
                AppendText(html.AsSpan(tagStart, 1));
                position = tagStart + 1;
                continue;
            }
            position = tagEnd;

            if (!name.Equals("bts-term", StringComparison.OrdinalIgnoreCase))
                continue;

            if (closing)
            {
                if (termDepth == 0)
                    throw new InvalidOperationException("Unbalanced dictionary tag in translation result.");
                termDepth--;
                continue;
            }

            var id = ReadId(attributes);
            if (id < 0 || id >= replacements.Count)
                throw new InvalidOperationException("Invalid dictionary ID in translation result.");

            if (termDepth == 0 && !emitted[id])
            {
                output.Append(replacements[id]);
                emitted[id] = true;
            }
            if (!selfClosing)
                termDepth++;
        }

        if (termDepth != 0 || emitted.Any(static value => !value))
            throw new InvalidOperationException("Dictionary tags were not preserved in translation result.");
        return output.ToString();

        void AppendText(ReadOnlySpan<char> text)
        {
            if (termDepth == 0)
                output.Append(WebUtility.HtmlDecode(text.ToString()));
        }
    }

    private static bool TryReadTag(
        string html, int start, out int end, out string name, out string attributes,
        out bool closing, out bool selfClosing)
    {
        end = start;
        name = attributes = string.Empty;
        closing = selfClosing = false;

        var cursor = start + 1;
        if (cursor < html.Length && html[cursor] == '/')
        {
            closing = true;
            cursor++;
        }
        var nameStart = cursor;
        while (cursor < html.Length && (char.IsLetterOrDigit(html[cursor]) || html[cursor] is '-' or ':'))
            cursor++;
        if (cursor == nameStart)
            return false;
        name = html[nameStart..cursor];
        var attributesStart = cursor;
        char quote = '\0';
        while (cursor < html.Length)
        {
            var character = html[cursor];
            if (quote != '\0')
            {
                if (character == quote)
                    quote = '\0';
            }
            else if (character is '\'' or '"')
                quote = character;
            else if (character == '>')
            {
                var last = cursor - 1;
                while (last >= attributesStart && char.IsWhiteSpace(html[last]))
                    last--;
                selfClosing = last >= attributesStart && html[last] == '/';
                attributes = html[attributesStart..cursor];
                end = cursor + 1;
                return true;
            }
            cursor++;
        }
        return false;
    }

    private static int ReadId(string attributes)
    {
        var cursor = 0;
        while (cursor < attributes.Length)
        {
            while (cursor < attributes.Length && char.IsWhiteSpace(attributes[cursor]))
                cursor++;
            var nameStart = cursor;
            while (cursor < attributes.Length &&
                   (char.IsLetterOrDigit(attributes[cursor]) || attributes[cursor] is '-' or '_' or ':'))
                cursor++;
            if (cursor == nameStart)
            {
                cursor++;
                continue;
            }
            var name = attributes[nameStart..cursor];
            while (cursor < attributes.Length && char.IsWhiteSpace(attributes[cursor]))
                cursor++;
            if (cursor >= attributes.Length || attributes[cursor] != '=')
                continue;
            cursor++;
            while (cursor < attributes.Length && char.IsWhiteSpace(attributes[cursor]))
                cursor++;
            var quote = cursor < attributes.Length && (attributes[cursor] is '\'' or '"')
                ? attributes[cursor++] : '\0';
            var valueStart = cursor;
            if (quote != '\0')
            {
                while (cursor < attributes.Length && attributes[cursor] != quote)
                    cursor++;
            }
            else
            {
                while (cursor < attributes.Length && !char.IsWhiteSpace(attributes[cursor]) && attributes[cursor] != '/')
                    cursor++;
            }
            var value = attributes[valueStart..cursor];
            if (quote != '\0' && cursor < attributes.Length)
                cursor++;
            if (name.Equals("data-id", StringComparison.OrdinalIgnoreCase))
                return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : -1;
        }
        return -1;
    }
}
