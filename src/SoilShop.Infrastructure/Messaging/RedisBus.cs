using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace SoilShop.Infrastructure;

public sealed class RedisBus(IConfiguration config) : IDisposable
{
    public const string Stream = "soilshop:events";
    public const string Group = "soilshop-worker";
    public const string DeadLetterStream = "soilshop:events:dead-letter";
    private ConnectionMultiplexer? connection;
    public async Task<IDatabase> DatabaseAsync()
    {
        if (connection is null) connection = await ConnectionMultiplexer.ConnectAsync(config.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false");
        return connection.GetDatabase();
    }
    public void Dispose() => connection?.Dispose();
}
