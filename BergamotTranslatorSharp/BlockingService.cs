using System.Runtime.InteropServices;

namespace BergamotTranslatorSharp;

public sealed class BlockingService : IDisposable
{
    private IntPtr translator;
    private bool disposedValue;

    // P/Invoke定義
    [DllImport("bergamot", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr translator_initialize([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] configPaths, int count);

    [DllImport("bergamot", CallingConvention = CallingConvention.Cdecl)]
    private static extern void translator_free(IntPtr translator);

    [DllImport("bergamot", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    private static extern string translator_translate(IntPtr translator, string text);

    public BlockingService(params string[] configPaths)
    {
        translator = translator_initialize(configPaths, configPaths.Length);
        if (translator == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create translator instance");
        }
    }

    public string Translate(string text)
    {
        if (disposedValue)
            throw new ObjectDisposedException(nameof(BlockingService));
        return translator_translate(translator, text);
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
            translator_free(translator);
            translator = IntPtr.Zero;
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
