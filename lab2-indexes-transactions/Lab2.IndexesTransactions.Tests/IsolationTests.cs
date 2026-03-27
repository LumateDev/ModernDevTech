using Lab2.IndexesTransactions.Tests.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lab2.IndexesTransactions.Tests;

public class IsolationTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public IsolationTests(IntegrationTestFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task ReadCommittedShouldPreventDirtyReads()
    {
        // ── 1. Подготовка ──
        _output.WriteLine("═══ ПОДГОТОВКА: Создание счетов ═══");
        await _client.PostAsync("/api/transaction/seed", null);
        await _client.PostAsync("/api/transaction/reset", null);
        _output.WriteLine("  ✓ Счета созданы и сброшены");

        // ── 2. Демонстрация READ COMMITTED ──
        _output.WriteLine("═══ ДЕМОНСТРАЦИЯ: READ COMMITTED ═══");
        _output.WriteLine("  Сценарий:");
        _output.WriteLine("    TX1: UPDATE balance = 500 (НЕ коммитит)");
        _output.WriteLine("    TX2: SELECT balance → должен видеть СТАРОЕ значение (1000)");
        _output.WriteLine("    TX1: COMMIT");
        _output.WriteLine("    TX2: SELECT balance → теперь видит НОВОЕ значение (500)");

        var result = await _client.GetFromJsonAsync<IsolationResult>("/api/isolation/read-committed");

        _output.WriteLine($"  Результат: {result?.Explanation}");

        Assert.NotNull(result);
        Assert.Contains("No dirty reads", result.Explanation);
        _output.WriteLine("  ✓ Подтверждено: READ COMMITTED предотвращает грязное чтение (dirty reads)");
        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }

    [Fact]
    public async Task RepeatableReadShouldUseSnapshot()
    {
        // ── 1. Подготовка ──
        _output.WriteLine("═══ ПОДГОТОВКА: Создание счетов ═══");
        await _client.PostAsync("/api/transaction/seed", null);
        await _client.PostAsync("/api/transaction/reset", null);
        _output.WriteLine("  ✓ Счета созданы и сброшены");

        // ── 2. Демонстрация REPEATABLE READ ──
        _output.WriteLine("═══ ДЕМОНСТРАЦИЯ: REPEATABLE READ ═══");
        _output.WriteLine("  Сценарий:");
        _output.WriteLine("    TX1: UPDATE balance = 500 (НЕ коммитит)");
        _output.WriteLine("    TX2: SELECT balance → видит СТАРОЕ значение (1000)");
        _output.WriteLine("    TX1: COMMIT");
        _output.WriteLine("    TX2: SELECT balance → ВСЁ РАВНО видит старое значение (1000)");
        _output.WriteLine("    (потому что снимок зафиксирован на момент начала TX2)");

        var result = await _client.GetFromJsonAsync<IsolationResult>("/api/isolation/repeatable-read");

        _output.WriteLine($"  Результат: {result?.Explanation}");

        Assert.NotNull(result);
        Assert.Contains("both times", result.Explanation);
        _output.WriteLine("  ✓ Подтверждено: REPEATABLE READ использует снимок (snapshot)");
        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }
}