using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;
using Xunit;

namespace BakToBucket.Tests.Integration.Infrastructure;

public sealed class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; }

    public PostgreSqlDatabaseFixture()
    {
        Container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("bak_to_bucket")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public ValueTask InitializeAsync() => new ValueTask(Container.StartAsync());

    public ValueTask DisposeAsync() => Container.DisposeAsync();
}
