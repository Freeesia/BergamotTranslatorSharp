using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace BergamotTranslatorSharp.Tests;

public sealed class TranslatorModelFixture : IAsyncLifetime
{
    private const string ModelBaseUrl = "https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data";
    private static readonly HttpClient HttpClient = new();
    private static readonly ModelPack[] ModelPacks =
    [
        CreatePack(
            "en-kn",
            "h1-2025_DDVsjRwsQ8GL3-JAiAmmfQ",
            "enkn",
            "17a5ddc86e24f62c04aa15272930275812b5c1674bec50a877528680d5834d2d"),
        CreatePack(
            "kn-en",
            "h1-2025_Ql24XjW0Sp-zWsmLrV5TGQ",
            "knen",
            "49e8d3bd794b098047f7b177d9d0e6d3e49ac798c2b75bc132149cb46c5bfbfd"),
    ];

    private readonly Dictionary<string, string> configurationPaths = new(StringComparer.Ordinal);

    public string ConfigurationFor(string direction) => configurationPaths[direction];

    public async Task InitializeAsync()
    {
        var cacheRoot = Environment.GetEnvironmentVariable("BERGAMOT_TEST_MODEL_CACHE");
        if (string.IsNullOrWhiteSpace(cacheRoot))
        {
            cacheRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BergamotTranslatorSharp",
                "test-model-cache");
        }

        foreach (var pack in ModelPacks)
        {
            var modelDirectory = Path.Combine(cacheRoot, pack.Direction);
            Directory.CreateDirectory(modelDirectory);
            foreach (var file in pack.Files)
                await EnsureModelFileAsync(file, modelDirectory);

            var configPath = Path.Combine(modelDirectory, "config.yml");
            await File.WriteAllTextAsync(configPath, pack.CreateConfiguration(), new UTF8Encoding(false));
            configurationPaths.Add(pack.Direction, configPath);
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ModelPack CreatePack(string direction, string modelId, string code, string modelSha256)
    {
        var directory = $"models/{direction}/{modelId}/exported";
        return new ModelPack(
            direction,
            new ModelFile($"{directory}/model.{code}.intgemm.alphas.bin.gz", modelSha256),
            new ModelFile($"{directory}/vocab.{code}.spm.gz"),
            new ModelFile($"{directory}/lex.50.50.{code}.s2t.bin.gz"));
    }

    private static async Task EnsureModelFileAsync(ModelFile file, string modelDirectory)
    {
        var outputPath = Path.Combine(modelDirectory, file.OutputFileName);
        if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 0 &&
            (file.Sha256 is null || await HasExpectedHashAsync(outputPath, file.Sha256)))
        {
            return;
        }

        var compressedPath = outputPath + ".gz";
        var temporaryCompressedPath = compressedPath + ".download";
        var temporaryOutputPath = outputPath + ".download";

        try
        {
            using var response = await HttpClient.GetAsync(
                $"{ModelBaseUrl}/{file.RemotePath}",
                HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            await using (var remoteStream = await response.Content.ReadAsStreamAsync())
            await using (var localStream = File.Create(temporaryCompressedPath))
                await remoteStream.CopyToAsync(localStream);

            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var compressedStream = File.OpenRead(temporaryCompressedPath))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            await using (var outputStream = File.Create(temporaryOutputPath))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await gzipStream.ReadAsync(buffer.AsMemory())) > 0)
                {
                    hash.AppendData(buffer, 0, read);
                    await outputStream.WriteAsync(buffer.AsMemory(0, read));
                }
            }

            if (file.Sha256 is not null)
            {
                var actualHash = Convert.ToHexString(hash.GetHashAndReset());
                if (!actualHash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"SHA-256 mismatch for model file {file.RemotePath}");
            }

            File.Move(temporaryOutputPath, outputPath, overwrite: true);
            File.Move(temporaryCompressedPath, compressedPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryCompressedPath))
                File.Delete(temporaryCompressedPath);
            if (File.Exists(temporaryOutputPath))
                File.Delete(temporaryOutputPath);
        }
    }

    private static async Task<bool> HasExpectedHashAsync(string path, string expectedHash)
    {
        await using var stream = File.OpenRead(path);
        var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream));
        return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ModelFile(string RemotePath, string? Sha256 = null)
    {
        public string OutputFileName => Path.GetFileNameWithoutExtension(RemotePath);
    }

    private sealed record ModelPack(
        string Direction,
        ModelFile Model,
        ModelFile Vocabulary,
        ModelFile Shortlist)
    {
        public ModelFile[] Files => [Model, Vocabulary, Shortlist];

        public string CreateConfiguration() => $$"""
            relative-paths: true
            models:
            - {{Model.OutputFileName}}
            vocabs:
            - {{Vocabulary.OutputFileName}}
            shortlist:
            - {{Shortlist.OutputFileName}}
            - false
            beam-size: 1
            normalize: 1.0
            word-penalty: 0
            max-length-break: 128
            mini-batch-words: 1024
            workspace: 128
            max-length-factor: 2.0
            skip-cost: true
            cpu-threads: 0
            quiet: true
            quiet-translation: true
            gemm-precision: int8shiftAlphaAll
            """;
    }
}
