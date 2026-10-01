using Xunit;

namespace BergamotTranslatorSharp.Tests;

// Loading libbergamot initializes tcmalloc's process-wide allocator on macOS.
// Keep that initialization from overlapping other test collections' allocations.
[CollectionDefinition(nameof(TranslatorModelCollection), DisableParallelization = true)]
public sealed class TranslatorModelCollection : ICollectionFixture<TranslatorModelFixture>;
