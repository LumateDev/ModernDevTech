using Lab3.Replication.Models;
using Lab3.Replication.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab3.Replication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReplicationController : ControllerBase
{
    private readonly InMemoryStore _store;
    private readonly ReplicationService _replication;
    private readonly NodeHealthService _health;

    public ReplicationController(InMemoryStore store, ReplicationService replication, NodeHealthService health)
    {
        _store = store;
        _replication = replication;
        _health = health;
    }

    /// <summary>
    /// Принять реплицированные данные (вызывается мастером)
    /// </summary>
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ReplicationMessage message)
    {
        if (!_health.IsHealthy)
            return StatusCode(503, new { error = "Node is offline", nodeId = _store.NodeId });

        // Имитация сетевой задержки при получении репликации
        if (_health.ArtificialDelayMs > 0)
            await Task.Delay(_health.ArtificialDelayMs);

        var applied = _store.ApplyReplicated(message);

        return Ok(new
        {
            nodeId = _store.NodeId,
            applied,
            key = message.Key,
            version = message.Version
        });
    }

    /// <summary>
    /// Полный дамп данных (для синхронизации восстановленной реплики)
    /// </summary>
    [HttpGet("dump")]
    public IActionResult Dump()
    {
        if (!_health.IsHealthy)
            return StatusCode(503, new { error = "Node is offline", nodeId = _store.NodeId });

        return Ok(new BulkSyncResponse
        {
            NodeId = _store.NodeId,
            Entries = _store.GetAll()
        });
    }

    /// <summary>
    /// Синхронизировать данные с мастера (вызывается на реплике)
    /// </summary>
    [HttpPost("sync-from-master")]
    public async Task<IActionResult> SyncFromMaster([FromQuery] string masterUrl)
    {
        if (!_health.IsHealthy)
            return StatusCode(503, new { error = "Node is offline", nodeId = _store.NodeId });

        var syncData = await _replication.FetchAllFromMaster(masterUrl);
        if (syncData == null)
            return StatusCode(502, new { error = "Failed to fetch data from master", masterUrl });

        int applied = 0;
        foreach (var entry in syncData.Entries)
        {
            var msg = new ReplicationMessage
            {
                Action = "PUT",
                Key = entry.Key,
                Value = entry.Value,
                Version = entry.Version,
                Timestamp = entry.Timestamp,
                Origin = entry.Origin
            };

            if (_store.ApplyReplicated(msg))
                applied++;
        }

        return Ok(new
        {
            nodeId = _store.NodeId,
            syncedFrom = syncData.NodeId,
            totalEntries = syncData.Entries.Count,
            applied
        });
    }
}