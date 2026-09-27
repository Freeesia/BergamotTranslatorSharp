using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BergamotTranslatorSharp.Tool;

internal sealed class ModelStore
{
    private static readonly Uri DefaultRegistryUri = new(
        "https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data/db/models.json");
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromMinutes(10) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string cacheDirectory;

    public ModelStore()
    {
        cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BergamotTranslatorSharp", "Tool");
    }

    public async Task<string[]> GetConfigurationPathsAsync(
        string source, string target, CancellationToken cancellationToken)
    {
        source = NormalizeLanguage(source);
        target = NormalizeLanguage(target);
        if (source == target)
            throw new ArgumentException("Source and target languages must differ.");

        var registry = await LoadRegistryAsync(cancellationToken);
        if (FindCandidates(registry, source, target) is not null)
            return [await PrepareAsync(registry, source, target, cancellationToken)];

        if (source == "en" || target == "en")
            throw new ArgumentException($"No model is available for {source}-{target}.");

        var first = await PrepareAsync(registry, source, "en", cancellationToken);
        var second = await PrepareAsync(registry, "en", target, cancellationToken);
        return [first, second];
    }

    private async Task<Registry> LoadRegistryAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(cacheDirectory);
        var path = Path.Combine(cacheDirectory, "models.json");
        if (File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < TimeSpan.FromDays(1))
            return DeserializeRegistry(await File.ReadAllTextAsync(path, cancellationToken));

        try
        {
            using var response = await SharedClient.GetAsync(DefaultRegistryUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var registry = DeserializeRegistry(json);
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
                File.Move(temporaryPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            return registry;
        }
        catch (HttpRequestException) when (File.Exists(path))
        {
            return DeserializeRegistry(await File.ReadAllTextAsync(path, cancellationToken));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && File.Exists(path))
        {
            return DeserializeRegistry(await File.ReadAllTextAsync(path, cancellationToken));
        }
    }

    private static Registry DeserializeRegistry(string json)
    {
        try
        {
            var registry = JsonSerializer.Deserialize<Registry>(json, JsonOptions);
            if (registry?.Models is null || registry.Models.Count == 0 ||
                !Uri.TryCreate(registry.BaseUrl, UriKind.Absolute, out var baseUri) ||
                baseUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("Mozilla model registry is invalid.");
            return registry;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Mozilla model registry is invalid.", exception);
        }
    }

    private async Task<string> PrepareAsync(Registry registry, string source, string target,
        CancellationToken cancellationToken)
    {
        var candidates = FindCandidates(registry, source, target)
            ?? throw new ArgumentException($"No model is available for {source}-{target}.");
        if (candidates.Length == 0)
            throw new ArgumentException($"No model is available for {source}-{target}.");
        var candidate = candidates.FirstOrDefault(static model => model.ReleaseStatus == "Release") ?? candidates[0];
        var files = candidate.Files ?? throw new InvalidDataException("Model has no files.");
        var model = files.Model ?? throw new InvalidDataException("Model file is missing.");
        var sourceVocab = files.SrcVocab ?? files.Vocab
            ?? throw new InvalidDataException("Source vocabulary is missing.");
        var targetVocab = files.TrgVocab ?? files.Vocab
            ?? throw new InvalidDataException("Target vocabulary is missing.");
        var shortlist = files.LexicalShortlist
            ?? throw new InvalidDataException("Lexical shortlist is missing.");

        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(model.Path)))[..16];
        var directory = Path.Combine(cacheDirectory, "models", $"{source}-{target}", key);
        Directory.CreateDirectory(directory);
        var baseUri = new Uri(registry.BaseUrl.TrimEnd('/') + "/");
        var modelName = await EnsureFileAsync(baseUri, model, directory, cancellationToken);
        var sourceVocabName = await EnsureFileAsync(baseUri, sourceVocab, directory, cancellationToken);
        var targetVocabName = await EnsureFileAsync(baseUri, targetVocab, directory, cancellationToken);
        var shortlistName = await EnsureFileAsync(baseUri, shortlist, directory, cancellationToken);

        var configPath = Path.Combine(directory, "config.yml");
        var precision = modelName.Contains("alphas", StringComparison.OrdinalIgnoreCase)
            ? "int8shiftAlphaAll" : "int8shiftAll";
        var configuration = $$"""
            relative-paths: true
            models:
            - {{modelName}}
            vocabs:
            - {{sourceVocabName}}
            - {{targetVocabName}}
            shortlist:
            - {{shortlistName}}
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
            gemm-precision: {{precision}}
            """;
        var temporaryConfig = configPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryConfig, configuration, new UTF8Encoding(false), cancellationToken);
            File.Move(temporaryConfig, configPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryConfig)) File.Delete(temporaryConfig);
        }
        return configPath;
    }

    private async Task<string> EnsureFileAsync(Uri baseUri, ModelFile file, string directory,
        CancellationToken cancellationToken)
    {
        var name = GetFileName(file.Path);
        var outputPath = Path.Combine(directory, name);
        if (await IsValidAsync(outputPath, file, cancellationToken))
            return name;

        var temporaryPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Console.Error.WriteLine($"Downloading {file.Path}");
            using var response = await SharedClient.GetAsync(new Uri(baseUri, file.Path),
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var gzip = new GZipStream(remote, CompressionMode.Decompress);
            await using (var output = File.Create(temporaryPath))
                await gzip.CopyToAsync(output, cancellationToken);

            if (!await IsValidAsync(temporaryPath, file, cancellationToken))
                throw new InvalidDataException($"Downloaded model file failed validation: {file.Path}");
            File.Move(temporaryPath, outputPath, overwrite: true);
            return name;
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static async Task<bool> IsValidAsync(string path, ModelFile file,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return false;
        var length = new FileInfo(path).Length;
        if (length == 0 || file.UncompressedSize is long expectedSize && length != expectedSize)
            return false;
        if (file.UncompressedHash is null) return true;
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
        return hash.Equals(file.UncompressedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFileName(string path)
    {
        var parts = path.Split('/');
        if (parts.Length < 2 || parts.Any(static part => part.Length == 0 || part == "." || part == ".." ||
            part.Any(static character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '-' or '_'))) ||
            !path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Model registry contains an invalid file path.");
        return Path.GetFileNameWithoutExtension(parts[^1]);
    }

    private static string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language) ||
            language.Any(static character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
            throw new ArgumentException("Language codes must contain only letters, digits, hyphens, or underscores.");
        return language.ToLowerInvariant();
    }

    private static ModelCandidate[]? FindCandidates(Registry registry, string source, string target)
    {
        var direction = $"{source}-{target}";
        return registry.Models.FirstOrDefault(entry =>
            entry.Key.Equals(direction, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private sealed class Registry
    {
        public string BaseUrl { get; set; } = "";
        public Dictionary<string, ModelCandidate[]> Models { get; set; } = [];
    }

    private sealed class ModelCandidate
    {
        public string? ReleaseStatus { get; set; }
        public ModelFiles? Files { get; set; }
    }

    private sealed class ModelFiles
    {
        public ModelFile? Model { get; set; }
        public ModelFile? Vocab { get; set; }
        public ModelFile? SrcVocab { get; set; }
        public ModelFile? TrgVocab { get; set; }
        public ModelFile? LexicalShortlist { get; set; }
    }

    private sealed class ModelFile
    {
        public string Path { get; set; } = "";
        public long? UncompressedSize { get; set; }
        public string? UncompressedHash { get; set; }
    }
}
