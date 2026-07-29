using Xunit;

namespace BergamotTranslatorSharp.Tests;

public class BlockingServiceTests
{
    [Fact]
    public void BuildBatchHtml_UsesOnlyEntitiesSupportedByBergamot()
    {
        var html = BlockingService.BuildBatchHtml([
            "That's",
            "\"quoted\"",
            "Tom & Jerry",
            "<tag>",
            "literal &#39;",
        ]);

        Assert.Equal(
            "<p>That's</p><p>\"quoted\"</p><p>Tom &amp; Jerry</p>" +
            "<p>&lt;tag&gt;</p><p>literal &amp;#39;</p>",
            html);
    }

    [Fact]
    public void BuildBatchHtml_ConvertsNewlinesToBreakElements()
    {
        var html = BlockingService.BuildBatchHtml(["line 1\r\nline 2\nline 3"]);

        Assert.Equal("<p>line 1<br>line 2<br>line 3</p>", html);
    }
}
