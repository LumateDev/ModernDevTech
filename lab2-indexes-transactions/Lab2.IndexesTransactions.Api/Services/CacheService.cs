using System.Diagnostics;
using System.Text.Json;
using Lab2.IndexesTransactions.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Lab2.IndexesTransactions.Services;

public class CacheService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;

    public CacheService(IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
    }

    public async Task<object> GetUserAsync(int id)
    {
        var cache = _redis.GetDatabase();
        var cacheKey = $"user:{id}";
        var sw = Stopwatch.StartNew();

        // 1. Проверяем кэш
        var cached = await cache.StringGetAsync(cacheKey);
        if (cached.HasValue)
        {
            sw.Stop();
            var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(cached!);
            return new
            {
                source = "cache (Redis)",
                elapsed_ms = sw.Elapsed.TotalMilliseconds,
                data = userData
            };
        }

        // 2. Читаем из БД через EF Core
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        sw.Stop();

        if (user == null)
        {
            return new { source = "database", elapsed_ms = sw.Elapsed.TotalMilliseconds, data = (object?)null, message = "User not found" };
        }

        var result = new Dictionary<string, object>
        {
            ["id"] = user.Id,
            ["name"] = user.Name,
            ["email"] = user.Email
        };

        // 3. Помещаем в кэш (TTL 60 секунд)
        await cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromSeconds(60));

        return new
        {
            source = "database (saved to cache)",
            elapsed_ms = sw.Elapsed.TotalMilliseconds,
            data = result
        };
    }

    public async Task<object> ClearCacheAsync(int id)
    {
        var cache = _redis.GetDatabase();
        var deleted = await cache.KeyDeleteAsync($"user:{id}");
        return new { key = $"user:{id}", deleted };
    }

    public async Task<object> GetStatsAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints()[0]);
        var keyCount = await server.DatabaseSizeAsync();

        return new
        {
            redis_keys = keyCount
        };
    }
}