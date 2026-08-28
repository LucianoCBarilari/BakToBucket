using DotNet.Testcontainers.Builders;
using Testcontainers.Minio;
using Xunit;

namespace BakToBucket.Tests.Integration.Infrastructure;

public sealed class StorageFixture : IAsyncLifetime
{
    public MinioContainer Container { get; }

    public StorageFixture()
    {
        Container = new MinioBuilder()
            .WithImage("minio/minio:latest")
            .WithUsername("minioadmin")
            .WithPassword("minioadmin")
            .Build();
    }

    public ValueTask InitializeAsync() => new ValueTask(Container.StartAsync());

    public ValueTask DisposeAsync() => Container.DisposeAsync();
}
