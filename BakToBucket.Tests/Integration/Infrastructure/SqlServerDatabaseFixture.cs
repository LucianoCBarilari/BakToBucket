using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;
using Xunit;

namespace BakToBucket.Tests.Integration.Infrastructure;

public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    public MsSqlContainer Container { get; }

    public SqlServerDatabaseFixture()
    {
        Container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }

    public ValueTask InitializeAsync() => new ValueTask(Container.StartAsync());

    public ValueTask DisposeAsync() => Container.DisposeAsync();
}
