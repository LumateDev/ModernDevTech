using System.Diagnostics;
using Lab2.IndexesTransactions.Data;
using Lab2.IndexesTransactions.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lab2.IndexesTransactions.Services;

public class IndexDemoService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IndexDemoService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task SeedUsersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Users.CountAsync() >= 10000)
            return;

        var users = Enumerable.Range(1, 10000).Select(i => new User
        {
            Name = $"User_{i}",
            Email = $"user_{i}@example.com"
        }).ToList();

        db.Users.AddRange(users);
        await db.SaveChangesAsync();
    }

    public async Task<object> SearchWithoutIndexAsync(string email)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Удаляем индекс
        await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS idx_users_email");

        // EXPLAIN ANALYZE (только это требует raw SQL)
        var explain = await RunExplainAsync(db, email);

        // Поиск через EF Core
        var sw = Stopwatch.StartNew();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        sw.Stop();

        return new
        {
            method = "WITHOUT index (Seq Scan)",
            email,
            found = user != null,
            elapsed_ms = sw.Elapsed.TotalMilliseconds,
            explain
        };
    }

    public async Task<object> SearchWithIndexAsync(string email)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Создаём индекс
        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS idx_users_email ON users (email)");

        // EXPLAIN ANALYZE
        var explain = await RunExplainAsync(db, email);

        // Поиск через EF Core
        var sw = Stopwatch.StartNew();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        sw.Stop();

        return new
        {
            method = "WITH index (Index Scan)",
            email,
            found = user != null,
            elapsed_ms = sw.Elapsed.TotalMilliseconds,
            explain
        };
    }

    private async Task<List<string>> RunExplainAsync(AppDbContext db, string email)
    {
        var lines = new List<string>();
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync();

        await using var cmd = (NpgsqlCommand)conn.CreateCommand();
        cmd.CommandText = "EXPLAIN ANALYZE SELECT * FROM users WHERE email = @email";
        cmd.Parameters.AddWithValue("email", email);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lines.Add(reader.GetString(0));
        }

        return lines;
    }
}