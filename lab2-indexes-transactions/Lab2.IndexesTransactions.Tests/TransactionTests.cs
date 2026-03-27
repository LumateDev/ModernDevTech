using Lab2.IndexesTransactions.Tests.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lab2.IndexesTransactions.Tests;

public class TransactionTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public TransactionTests(IntegrationTestFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task TransferWithErrorShouldRollback()
    {
        // ── 1. Подготовка: создаём и сбрасываем счета ──
        _output.WriteLine("═══ ПОДГОТОВКА: Создание счетов (id=1 balance=1000, id=2 balance=1000) ═══");
        await _client.PostAsync("/api/transaction/seed", null);
        await _client.PostAsync("/api/transaction/reset", null);

        var initialBalances = await GetBalancesAsync();
        _output.WriteLine($"  Начальные балансы: {FormatBalances(initialBalances)}");

        // ── 2. Попытка перевода с ошибкой (Atomicity) ──
        _output.WriteLine("═══ ДЕЙСТВИЕ: Перевод 200 со счёта 1 на счёт 2 (с искусственной ошибкой) ═══");
        _output.WriteLine("  Ожидаемое поведение:");
        _output.WriteLine("    1. Списание 200 со счёта 1");
        _output.WriteLine("    2. ОШИБКА перед зачислением на счёт 2");
        _output.WriteLine("    3. ОТКАТ — все изменения отменяются (Atomicity)");

        var failResponse = await _client.PostAsync(
            "/api/transaction/transfer-fail?from=1&to=2&amount=200", null);
        var failBody = await failResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"  Ответ сервера: {failResponse.StatusCode}");
        _output.WriteLine($"  Тело ответа: {failBody}");

        // ── 3. Проверка: балансы НЕ изменились ──
        _output.WriteLine("═══ ПРОВЕРКА: Балансы должны остаться без изменений ═══");
        var afterBalances = await GetBalancesAsync();
        _output.WriteLine($"  Балансы после ошибки: {FormatBalances(afterBalances)}");

        Assert.Equal(initialBalances.Count, afterBalances.Count);
        for (int i = 0; i < initialBalances.Count; i++)
        {
            _output.WriteLine($"  Счёт {afterBalances[i].Id}: " +
                $"ожидалось={initialBalances[i].Balance}, " +
                $"фактически={afterBalances[i].Balance}");

            Assert.Equal(initialBalances[i].Balance, afterBalances[i].Balance);
        }

        _output.WriteLine("  ✓ Подтверждено: свойство Atomicity — при ошибке все изменения откатываются");
        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }

    private async Task<List<AccountDto>> GetBalancesAsync()
    {
        var response = await _client.GetAsync("/api/transaction/balances");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AccountDto>>() ?? new();
    }

    private static string FormatBalances(List<AccountDto> balances)
    {
        return string.Join(", ", balances.ConvertAll(b => $"Счёт[{b.Id}]={b.Balance}"));
    }
}