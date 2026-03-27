using Lab2.IndexesTransactions.Tests.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lab2.IndexesTransactions.Tests;

public class CacheTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public CacheTests(IntegrationTestFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task CacheAside_SecondRequestShouldComeFromCache()
    {
        // ── 1. Подготовка: чистим кэш и наполняем БД ──
        _output.WriteLine("═══ ПОДГОТОВКА: Очистка кэша и наполнение БД ═══");
        await _client.DeleteAsync("/api/cache/user/1");
        await _client.PostAsync("/api/indexdemo/seed", null);
        _output.WriteLine("  ✓ Кэш очищен, пользователи созданы");

        // ── 2. Первый запрос — данные из БД, сохраняются в Redis ──
        _output.WriteLine("═══ ЗАПРОС 1: Первое обращение (ожидаем чтение из БД) ═══");
        var firstResponse = await _client.GetAsync("/api/cache/user/1");
        firstResponse.EnsureSuccessStatusCode();
        var firstData = await firstResponse.Content.ReadFromJsonAsync<CacheResponse>();
        _output.WriteLine($"  Источник: {firstData!.Source}");
        Assert.Contains("database", firstData.Source);
        _output.WriteLine("  ✓ Подтверждено: данные получены из базы данных");
        _output.WriteLine("  → Данные сохранены в Redis (TTL 60 секунд)");

        // ── 3. Повторный запрос — данные из кэша ──
        _output.WriteLine("═══ ЗАПРОС 2: Повторное обращение (ожидаем чтение из Redis) ═══");
        var secondResponse = await _client.GetAsync("/api/cache/user/1");
        secondResponse.EnsureSuccessStatusCode();
        var secondData = await secondResponse.Content.ReadFromJsonAsync<CacheResponse>();
        _output.WriteLine($"  Источник: {secondData!.Source}");
        Assert.Equal("cache (Redis)", secondData.Source);
        _output.WriteLine("  ✓ Подтверждено: данные получены из кэша Redis");

        // ── 4. Итог ──
        _output.WriteLine("═══ ИТОГ ═══");
        _output.WriteLine("  Стратегия Cache Aside работает корректно:");
        _output.WriteLine("    1. Проверяется кэш → нет данных");
        _output.WriteLine("    2. Читаем из БД → помещаем в кэш");
        _output.WriteLine("    3. Следующий запрос → данные из кэша (БД не трогаем)");
        _output.WriteLine("  ✓ Снижение нагрузки на базу данных подтверждено");
        _output.WriteLine("═══ ТЕСТ ПРОЙДЕН ✓ ═══");
    }
}