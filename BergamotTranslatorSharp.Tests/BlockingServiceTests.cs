using Xunit;

namespace BergamotTranslatorSharp.Tests;

public sealed class BlockingServiceTests(TranslatorModelFixture models) : IClassFixture<TranslatorModelFixture>
{
    private static readonly string[] Inputs =
    [
        "Hello, world!",
        "First line.\nSecond line.",
        "Fish & chips <tag> > cheese.",
        "Café and 東京.",
    ];

    [Fact]
    public void TranslateMultiple_TranslatesEachPlainTextWithOneModel()
    {
        using var service = new BlockingService(models.ConfigurationFor("en-kn"));

        var expected = Inputs.Select(input => service.Translate(input)).ToArray();
        var actual = service.Translate(Inputs);

        Assert.Equal(expected, actual);
        Assert.All(actual, translation => Assert.False(string.IsNullOrWhiteSpace(translation)));
        Assert.NotEqual(Inputs[0], actual[0]);
        Assert.Empty(service.Translate(Array.Empty<string>()));
    }

    [Fact]
    public void TranslateMultiple_UsesTwoModelPivot()
    {
        using var service = new BlockingService(
            models.ConfigurationFor("en-kn"),
            models.ConfigurationFor("kn-en"));

        var expected = Inputs.Select(input => service.Translate(input)).ToArray();
        var actual = service.Translate(Inputs);

        Assert.Equal(expected, actual);
        Assert.All(actual, translation => Assert.False(string.IsNullOrWhiteSpace(translation)));
    }
}
