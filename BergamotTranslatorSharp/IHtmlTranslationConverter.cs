namespace BergamotTranslatorSharp;

/// <summary>Converts structured data to HTML and keeps the state needed to restore it.</summary>
/// <typeparam name="T">The type of the structured data.</typeparam>
public interface IHtmlTranslationConverter<T>
{
    IHtmlTranslationContext<T> ToHtml(T value);
}

/// <summary>A single conversion whose HTML can be translated and restored.</summary>
/// <typeparam name="T">The type of the structured data.</typeparam>
public interface IHtmlTranslationContext<T>
{
    string Html { get; }

    bool HasTranslatableContent { get; }

    T FromHtml(string translatedHtml);
}
