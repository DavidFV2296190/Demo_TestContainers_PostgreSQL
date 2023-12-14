using Testcontainers.PostgreSql;

namespace Dragons.Tests;

public sealed class ServiceDragonTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer m_postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    public Task InitializeAsync()
    {
        return m_postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return m_postgres.DisposeAsync().AsTask();
    }

    [Fact]
    public void DevraisRetournerDeuxDragons()
    {
        // Arranger
        var serviceDragon = new ServiceDragon(new DbConnectionProvider(m_postgres.GetConnectionString()));

        // Agir
        serviceDragon.Create(new Dragon(1, "Shenron", 850));
        serviceDragon.Create(new Dragon(2, "Smaug", 150));
        var dragons = serviceDragon.GetDragons();

        // Assert
        Assert.Equal(2, dragons.Count());
    }
}