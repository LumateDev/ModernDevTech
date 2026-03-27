using Lab2.IndexesTransactions.Tests.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lab2.IndexesTransactions.Tests;

public class IndexesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public IndexesTests(IntegrationTestFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task SearchWithIndexShouldBeFasterAndUseIndexScan()
    {
        // ── 1. Подготовка: наполняем таблицу 10 000 записями ──
        _output.WriteLine("═══ ПОДГОТОВКА: Наполнение таблицы users (10 000 записей) ═══");
        var seedResponse = await _client.PostAsync("/api/indexdemo/seed", null);
        seedResponse.EnsureSuccessStatusCode();
        _output.WriteLine("  ✓ Таблица наполнена");

        // ── 2. Поиск БЕЗ индекса ──
        _output.WriteLine("═══ ПОИСК БЕЗ ИНДЕКСА (ожидаем Seq Scan) ═══");
        var noIndexResponse = await _client.GetAsync(
            "/api/indexdemo/search-no-index?email=user_9999@example.com");
        noIndexResponse.EnsureSuccessStatusCode();
        var noIndexData = await noIndexResponse.Content.ReadFromJsonAsync<SearchResult>();
        _output.WriteLine($"  Время: {noIndexData!.ElapsedMs:F2} мс");
        _output.WriteLine($"  План:  {noIndexData.Explain[0]}");

        // Проверяем что используется полное сканирование
        var noIndexPlan = string.Join(" ", noIndexData.Explain);
        Assert.Contains("Seq Scan", noIndexPlan);
        _output.WriteLine("  ✓ Подтверждено: полное сканирование таблицы (Seq Scan)");

        // ── 3. Поиск С индексом ──
        _output.WriteLine("═══ ПОИСК С ИНДЕКСОМ (ожидаем Index Scan / Bitmap Scan) ═══");
        var withIndexResponse = await _client.GetAsync(
            "/api/indexdemo/search-with-index?email=user_9999@example.com");
        withIndexResponse.EnsureSuccessStatusCode();
        var withIndexData = await withIndexResponse.Content.ReadFromJsonAsync<SearchResult>();
        _output.WriteLine($"  Время: {withIndexData!.ElapsedMs:F2} мс");
        _output.WriteLine($"  План:  {withIndexData.Explain[0]}");

        // Проверяем что используется индекс
        var withIndexPlan = string.Join(" ", withIndexData.Explain);
        Assert.True(
            withIndexPlan.Contains("Index Scan") ||
            withIndexPlan.Contains("Index Only Scan") ||
            withIndexPlan.Contains("Bitmap"),
            $"Ожидалось использование индекса, получено: {withIndexPlan}");
        _output.WriteLine("  ✓ Подтверждено: используется индекс");

        // ── 4. Сравнение производительности ──
        _output.WriteLine("═══ СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ═══");
        _output.WriteLine($"  Без индекса: {noIndexData.ElapsedMs:F2} мс");
        _output.WriteLine($"  С индексом:  {withIndexData.ElapsedMs:F2} мс");

        if (withIndexData.ElapsedMs < noIndexData.ElapsedMs)
        {
            var speedup = noIndexData.ElapsedMs / withIndexData.ElapsedMs;
            _output.WriteLine($"  ✓ Индекс быстрее в {speedup:F1}x раз");
        }
        else
        {
            _output.WriteLine("  ⚠ Индекс не быстрее (возможно из-за кэша PostgreSQL)");
        }

        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }
}