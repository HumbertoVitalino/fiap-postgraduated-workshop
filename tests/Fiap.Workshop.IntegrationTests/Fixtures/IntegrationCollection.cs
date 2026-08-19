using Xunit;

namespace Fiap.Workshop.IntegrationTests.Fixtures;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection : ICollectionFixture<DatabaseFixture> { }
