using Lab2.IndexesTransactions.Data;
using Microsoft.EntityFrameworkCore;

namespace Lab2.IndexesTransactions.Services;

public class TransactionService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TransactionService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task SeedAccountsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Accounts.CountAsync() >= 2) return;

        db.Accounts.AddRange(
            new Models.Account { Id = 1, Balance = 1000m },
            new Models.Account { Id = 2, Balance = 1000m }
        );
        await db.SaveChangesAsync();
    }

    public async Task<object> TransferAsync(int fromId, int toId, decimal amount, bool simulateError)
    {
        var before = await GetBalancesAsync();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var fromAccount = await db.Accounts.FindAsync(fromId)
                ?? throw new Exception($"Account {fromId} not found");
            var toAccount = await db.Accounts.FindAsync(toId)
                ?? throw new Exception($"Account {toId} not found");

            // Списание
            fromAccount.Balance -= amount;
            await db.SaveChangesAsync();

            // Искусственная ошибка
            if (simulateError)
                throw new Exception("Simulated error after debit — before credit!");

            // Зачисление
            toAccount.Balance += amount;
            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            var after = await GetBalancesAsync();
            return new
            {
                status = "SUCCESS",
                before,
                after,
                message = $"Transferred {amount} from account {fromId} to account {toId}"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            var after = await GetBalancesAsync();
            return new
            {
                status = "ROLLED BACK",
                before,
                after,
                message = ex.Message,
                note = "Atomicity: balances unchanged after error"
            };
        }
    }

    public async Task<object> GetBalancesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Accounts
            .OrderBy(a => a.Id)
            .Select(a => new { a.Id, a.Balance })
            .ToListAsync();
    }

    public async Task ResetAccountsAsync()
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