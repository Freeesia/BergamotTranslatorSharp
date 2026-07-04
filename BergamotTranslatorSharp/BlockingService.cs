using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BergamotTranslatorSharp;

public sealed partial class BlockingService : IDisposable
{
    private IntPtr translator;
    private bool disposedValue;

    // P/Invoke定義
    [LibraryImport("bergamot", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial IntPtr translator_initialize(string[] configPaths, int count);

    [DllImport("bergamot", CallingConvention = CallingConvention.Cdecl)]
    private static extern void translator_free(IntPtr translator);

    [DllImport("bergamot", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    private static extern string translator_translate(
        IntPtr translator,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
        [MarshalAs(UnmanagedType.I1)] bool html);

    public BlockingService(params string[] configPaths)
    {
        ArgumentNullException.ThrowIfNull(configPaths);
        translator = translator_initialize(configPaths, configPaths.Length);

        if (translator == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create translator instance");
        }
    }

    public string Translate(string text, bool html = false)
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(BlockingService));
        return translator_translate(translator, text, html);
    }

    public string[] TranslateMultiple(IEnumerable<string> texts)
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(BlockingService));

        var textList = texts.ToList();
        if (textList.Count == 0)
            return [];

        // HTML-escape each text, replace newlines with <br>, wrap in <p>
        var html = string.Concat(textList.Select(t =>
            $"<p>{WebUtility.HtmlEncode(t).Replace("\r\n", "<br>").Replace("\n", "<br>")}</p>"));

        // Translate as HTML
        var translatedHtml = Translate(html, html: true);

        // Extract translated text from each <p> tag, convert <br> back to newlines, decode HTML entities
        return [.. Regex.Matches(translatedHtml, @"<p>(.*?)</p>", RegexOptions.Singleline)
            .Select(m => WebUtility.HtmlDecode(Regex.Replace(m.Groups[1].Value, @"<br\s*/?>", "\n")))];
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // マネージド状態を破棄します (マネージド オブジェクト)
            }

            // アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
            // 大きなフィールドを null に設定します
            if (translator != IntPtr.Zero)
            {
                translator_free(translator);
                translator = IntPtr.Zero;
            }
            disposedValue = true;
        }
    }

    ~BlockingService()
        => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
