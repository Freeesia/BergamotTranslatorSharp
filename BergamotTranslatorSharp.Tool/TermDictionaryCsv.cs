using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace BergamotTranslatorSharp.Tool;

internal static class TermDictionaryCsv
{
    public static IReadOnlyDictionary<string, string> Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var reader = new StreamReader(path, new UTF8Encoding(false, true),
            detectEncodingFromByteOrderMarks: true);
        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false,
        };
        parser.SetDelimiters(",");

        var terms = new Dictionary<string, string>(StringComparer.Ordinal);
        var recordNumber = 0;
        while (!parser.EndOfData)
        {
            string[] fields;
            try
            {
                fields = parser.ReadFields()
                    ?? throw new InvalidDataException("Dictionary CSV contains an invalid record.");
            }
            catch (MalformedLineException exception)
            {
                throw new InvalidDataException(
                    $"Dictionary CSV has malformed quoting at line {parser.ErrorLineNumber}.", exception);
            }

            recordNumber++;
            if (recordNumber == 1 && fields.Length == 2 &&
                fields[0].Equals("source", StringComparison.OrdinalIgnoreCase) &&
                fields[1].Equals("target", StringComparison.OrdinalIgnoreCase))
                continue;

            if (fields.Length != 2)
                throw new InvalidDataException(
                    $"Dictionary CSV record {recordNumber} must have exactly two columns.");
            if (fields[0].Length == 0)
                throw new InvalidDataException(
                    $"Dictionary CSV record {recordNumber} has an empty source term.");
            if (!terms.TryAdd(fields[0], fields[1]))
                throw new InvalidDataException(
                    $"Dictionary CSV record {recordNumber} repeats source term '{fields[0]}'.");
        }

        if (terms.Count == 0)
            throw new InvalidDataException("Dictionary CSV contains no terms.");
        return terms;
    }
}
