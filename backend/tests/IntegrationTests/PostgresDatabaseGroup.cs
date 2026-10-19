using Platform.TestSupport;

namespace Platform.IntegrationTests;

/// <summary>Shares one PostgreSQL container across the tests that need it.</summary>
[CollectionDefinition(Name)]
public sealed class PostgresDatabaseGroup : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "PostgreSQL";
}
