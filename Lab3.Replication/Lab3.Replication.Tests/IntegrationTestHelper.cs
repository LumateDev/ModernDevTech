using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Lab3.Replication.Tests;

/// <summary>
/// Вспомогательный класс для интеграционных тестов.
/// Тесты обращаются напрямую к запущенным контейнерам по HTTP.
/// </summary>
public static class TestHelper
{
    public static readonly string MasterUrl =
        Environment.GetEnvironmentVariable("MASTER_URL") ?? "http://localhost:8081";
    public static readonly string ReplicaBUrl =
        Environment.GetEnvironmentVariable("REPLICA_B_URL") ?? "http://localhost:8082";
    public static readonly string ReplicaCUrl =
        Environment.GetEnvironmentVariable("REPLICA_C_URL") ?? "http://localhost:8083";

    private static readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public static HttpClient Client => _client;

    /// <summary>
    /// Записать ключ на мастер
    /// </summary>
    public static async Task<HttpResponseMessage> PutOnMaster(string key, string value)
    {
        return await _client.PostAsJsonAsync($"{MasterUrl}/api/data/{key}", new { value });
    }

    /// <summary>
    /// Прочитать ключ с указанного узла
    /// </summary>
    public static async Task<HttpResponseMessage> GetFrom(string nodeUrl, string key)
    {
        return await _client.GetAsync($"{nodeUrl}/api/data/{key}");
    }

    /// <summary>
    /// Получить все данные узла
    /// </summary>
    public static async Task<HttpResponseMessage> GetAllFrom(string nodeUrl)
    {
        return await _client.GetAsync($"{nodeUrl}/api/data");
    }

    /// <summary>
    /// Выключить узел (имитация сбоя)
    /// </summary>
    public static async Task TakeOffline(string nodeUrl)
    {
        await _client.PostAsync($"{nodeUrl}/api/admin/take-offline", null);
    }

    /// <summary>
    /// Включить узел обратно
    /// </summary>
    public static async Task BringOnline(string nodeUrl)
    {
        await _client.PostAsync($"{nodeUrl}/api/admin/bring-online", null);
    }

    /// <summary>
    /// Установить задержку
    /// </summary>
    public static async Task SetDelay(string nodeUrl, int ms)
    {
        await _client.PostAsync($"{nodeUrl}/api/admin/set-delay?ms={ms}", null);
    }

    /// <summary>
    /// Очистить данные узла
    /// </summary>
    public static async Task ClearNode(string nodeUrl)
    {
        await _client.PostAsync($"{nodeUrl}/api/admin/clear", null);
    }

    /// <summary>
    /// Синхронизировать реплику с мастера
    /// </summary>
    public static async Task<HttpResponseMessage> SyncFromMaster(string replicaUrl, string masterUrl)
    {
        return await _client.PostAsync(
            $"{replicaUrl}/api/replication/sync-from-master?masterUrl={Uri.EscapeDataString(masterUrl)}",
            null);
    }

    /// <summary>
    /// Очистить все узлы перед тестом
    /// </summary>
    public static async Task ResetAll()
    {
        // Включаем все узлы
        await BringOnline(MasterUrl);
        await BringOnline(ReplicaBUrl);
        await BringOnline(ReplicaCUrl);

        // Убираем задержки
        await SetDelay(MasterUrl, 0);
        await SetDelay(ReplicaBUrl, 0);
        await SetDelay(ReplicaCUrl, 0);

        // Чистим данные
        await ClearNode(MasterUrl);
        await ClearNode(ReplicaBUrl);
        await ClearNode(ReplicaCUrl);

        // Даём время на стабилизацию
        await Task.Delay(200);
    }
}