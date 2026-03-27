using System.Data;
using Lab2.IndexesTransactions.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lab2.IndexesTransactions.Services;

public class IsolationDemoService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _connectionString;

    public IsolationDemoService(IServiceScopeFactory scopeFactory, string connectionString)
    {
        _scopeFactory = scopeFactory;
        _connectionString = connectionString;
    }

    public async Task<object> DemoReadCommittedAsync()
    {
        return await RunIsolationDemoAsync(IsolationLevel.ReadCommitted, "READ COMMITTED");
    }

    public async Task<object> DemoReadUncommittedAsync()
    {
        return await RunIsolationDemoAsync(IsolationLevel.ReadUncommitted,
            "READ UNCOMMITTED (PostgreSQL → READ COMMITTED)");
    }

    public async Task<object> DemoRepeatableReadAsync()
    {
        return await RunIsolationDemoAsync(IsolationLevel.RepeatableRead, "REPEATABLE READ");
    }

    public async Task<object> DemoSerializableAsync()
    {
        return await RunIsolationDemoAsync(IsolationLevel.Serializable, "SERIALIZABLE");
    }

    private async Task<object> RunIsolationDemoAsync(IsolationLevel level, string levelName)
    {
        // Сброс балансов через EF Core
        await ResetAsync();

        var results = new List<object>();

        // Две параллельные транзакции требуют два отдельных соединения
        await using var conn1 = new NpgsqlConnection(_connectionString);
        await conn1.OpenAsync();
        await using var tx1 = await conn1.BeginTransactionAsync(level);

        // TX1: изменяем баланс, НЕ коммитим
        await using var updateCmd = new NpgsqlCommand(
            "UPDATE accounts SET balance = 500.00 WHERE id = 1", conn1, tx1);
        await updateCmd.ExecuteNonQueryAsync();
        results.Add(new { step = 1, action = "TX1: UPDATE balance = 500 WHERE id = 1 (NOT COMMITTED)" });

        // TX2: читаем в другой транзакции
        await using var conn2 = new NpgsqlConnection(_connectionString);
        await conn2.OpenAsync();
        await using var tx2 = await conn2.BeginTransactionAsync(level);

        await using var readCmd1 = new NpgsqlCommand(
            "SELECT balance FROM accounts WHERE id = 1", conn2, tx2);
        var balanceDuringTx1 = (decimal)(await readCmd1.ExecuteScalarAsync())!;
        results.Add(new
        {
            step = 2,
            action = "TX2: SELECT balance WHERE id = 1 (while TX1 not committed)",
            balance_seen = balanceDuringTx1,
            sees_uncommitted = balanceDuringTx1 == 500m
        });

        // TX1: коммитим
        await tx1.CommitAsync();
        results.Add(new { step = 3, action = "TX1: COMMIT" });

        // TX2: читаем снова
        await using var readCmd2 = new NpgsqlCommand(
            "SELECT balance FROM accounts WHERE id = 1", conn2, tx2);
        var balanceAfterCommit = (decimal)(await readCmd2.ExecuteScalarAsync())!;
        results.Add(new
        {
            step = 4,
            action = "TX2: SELECT balance WHERE id = 1 (after TX1 committed)",
            balance_seen = balanceAfterCommit,
            sees_committed_change = balanceAfterCommit == 500m
        });

        await tx2.CommitAsync();

        return new
        {
            isolation_level = levelName,
            steps = results,
            explanation = GetExplanation(level, balanceDuringTx1, balanceAfterCommit)
        };
    }

    private string GetExplanation(IsolationLevel level, decimal during, decimal after)
    {
        return level switch
        {
            IsolationLevel.ReadCommitted or IsolationLevel.ReadUncommitted =>
                $"READ COMMITTED: TX2 saw {during} (original) before commit, {after} after commit. No dirty reads.",
            IsolationLevel.RepeatableRead =>
                $"REPEATABLE READ: TX2 saw {during} both times. Snapshot taken at start of TX2.",
            IsolationLevel.Serializable =>
                $"SERIALIZABLE: TX2 saw {during} both times. Full isolation.",
            _ => "Unknown level"
        };
    }

    private async Task ResetAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var accounts = await db.Accounts.ToListAsync();
        foreach (var account in accounts)
        {
            account.Balance = 1000m;
        }
        await db.SaveChangesAsync();
    }
}