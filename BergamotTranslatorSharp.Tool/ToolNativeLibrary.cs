using System.Reflection;
using System.Runtime.InteropServices;
using BergamotTranslatorSharp;

namespace BergamotTranslatorSharp.Tool;

internal static class ToolNativeLibrary
{
    public static void Configure()
    {
        NativeLibrary.SetDllImportResolver(typeof(BlockingService).Assembly, Resolve);
    }

    private static nint Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "bergamot") return nint.Zero;

        var architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException("This CPU architecture is not supported.")
        };
        var platform = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsLinux() ? "linux"
            : OperatingSystem.IsMacOS() ? "osx"
            : throw new PlatformNotSupportedException("This operating system is not supported.");
        var fileName = platform switch
        {
            "win" => "bergamot.dll",
            "linux" => "libbergamot.so",
            _ => "libbergamot.dylib"
        };
        var path = Path.Combine(AppContext.BaseDirectory, "native", $"{platform}-{architecture}", fileName);
        return NativeLibrary.Load(path);
    }
}
