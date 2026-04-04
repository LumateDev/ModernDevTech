using Lab3.Replication.Models;
using System.Net.Http.Json;

namespace Lab3.Replication.Services;

/// <summary>
/// Сервис репликации — мастер рассылает изменения на реплики
/// </summary>
public class ReplicationService
{
    private readonly InMemoryStore _store;
    private readonly NodeHealthService _health;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly List<string> _replicaUrls;
    private readonly ILogger<ReplicationService> _logger;

    public ReplicationService(
        InMemoryStore store,
        NodeHealthService health,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<ReplicationService> logger)
    {
        _store = store;
        _health = health;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        // Список URL реплик (задаётся через переменную окружения)
        var replicaUrlsStr = config["ReplicaUrls"] ?? "";
        _replicaUrls = replicaUrlsStr
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .ToList();
    }

    /// <summary>
    /// Отправить изменение на все реплики (асинхронно, fire-and-forget с логированием)
    /// </summary>
    public async Task<ReplicationReport> ReplicateAsync(ReplicationMessage message)
    {
        var report = new ReplicationReport
        {
            TotalReplicas = _replicaUrls.Count,
            Results = new List<ReplicaResult>()
        };

        var tasks = _replicaUrls.Select(async url =>
        {
            var result = new ReplicaResult { Url = url };
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.PostAsJsonAsync($"{url}/api/replication/apply", message);

                result.Success = response.IsSuccessStatusCode;
                result.StatusCode = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    result.Error = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                _logger.LogWarning("Replication to {Url} failed: {Error}", url, ex.Message);
            }

            return result;
        });

        report.Results = (await Task.WhenAll(tasks)).ToList();
        report.SuccessCount = report.Results.Count(r => r.Success);

        return report;
    }

    /// <summary>
    /// Получить все данные (для синхронизации при восстановлении)
    /// </summary>
    public async Task<BulkSyncResponse?> FetchAllFromMaster(string masterUrl)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            return await client.GetFromJsonAsync<BulkSyncResponse>($"{masterUrl}/api/replication/dump");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to sync from master {Url}: {Error}", masterUrl, ex.Message);
            return null;
        }
    }

    public List<string> GetReplicaUrls() => _replicaUrls;
}

public class ReplicationReport
{
    public int TotalReplicas { get; set; }
    public int SuccessCount { get; set; }
    public List<ReplicaResult> Results { get; set; } = new();
}

public class ReplicaResult
{
    public string Url { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? Error { get; set; }
}