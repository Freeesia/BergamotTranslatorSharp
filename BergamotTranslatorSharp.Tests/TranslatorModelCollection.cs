using Xunit;

namespace BergamotTranslatorSharp.Tests;

[CollectionDefinition(nameof(TranslatorModelCollection))]
public sealed class TranslatorModelCollection : ICollectionFixture<TranslatorModelFixture>;
