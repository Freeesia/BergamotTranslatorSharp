using System.Runtime.InteropServices;
using Xunit;

namespace BergamotTranslatorSharp.Tests;

public class BlockingServiceTests
{
    [Fact]
    public void ReadTranslations_PreservesOrderAndPlainTextContents()
    {
        var expected = new[] { "second\nline", "first & <tag> > last" };
        var resultArray = Marshal.AllocHGlobal(expected.Length * IntPtr.Size);
        var resultPointers = new IntPtr[expected.Length];

        try
        {
            for (var i = 0; i < expected.Length; i++)
            {
                resultPointers[i] = Marshal.StringToCoTaskMemUTF8(expected[i]);
                Marshal.WriteIntPtr(resultArray, i * IntPtr.Size, resultPointers[i]);
            }

            Assert.Equal(expected, BlockingService.ReadTranslations(resultArray, expected.Length));
        }
        finally
        {
            foreach (var resultPointer in resultPointers)
                Marshal.FreeCoTaskMem(resultPointer);
            Marshal.FreeHGlobal(resultArray);
        }
    }
}
