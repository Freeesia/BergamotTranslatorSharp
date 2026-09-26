using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    [LibraryImport("bergamot", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial IntPtr translator_translate_multiple(
        IntPtr translator,
        string[] texts,
        nuint count);

    [DllImport("bergamot", CallingConvention = CallingConvention.Cdecl)]
    private static extern void translator_free_translations(IntPtr translations);

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

    public string[] Translate(IEnumerable<string> texts)
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(BlockingService));

        ArgumentNullException.ThrowIfNull(texts);

        var textList = texts.ToArray();
        if (textList.Length == 0)
            return [];

        if (textList.Any(static text => text is null))
            throw new ArgumentException("Batch input cannot contain null values.", nameof(texts));

        var translations = translator_translate_multiple(translator, textList, (nuint)textList.Length);
        if (translations == IntPtr.Zero)
            throw new InvalidOperationException("Failed to translate batch");

        try
        {
            var result = new string[textList.Length];
            for (var i = 0; i < textList.Length; i++)
            {
                var translatedText = Marshal.ReadIntPtr(translations, i * IntPtr.Size);
                result[i] = Marshal.PtrToStringUTF8(translatedText)
                    ?? throw new InvalidOperationException($"Native translation result {i} is null");
            }

            return result;
        }
        finally
        {
            translator_free_translations(translations);
        }
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
