using Lab3.Replication.Models;
using Lab3.Replication.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab3.Replication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly InMemoryStore _store;
    private readonly ReplicationService _replication;
    private readonly NodeHealthService _health;

    public DataController(InMemoryStore store, ReplicationService replication, NodeHealthService health)
    {
        _store = store;
        _replication = replication;
        _health = health;
    }

    /// <summary>
    /// Записать данные (только на мастере)
    /// </summary>
    [HttpPost("{key}")]
    public async Task<IActionResult> Put(string key, [FromBody] ValueRequest request)
    {
        if (!_health.IsHealthy)
            return StatusCode(503, new { error = "Node is offline", nodeId = _store.NodeId });

        if (!_store.IsMaster)
            return StatusCode(403, new
            {
                error = "This node is a replica. Writes are only accepted by the master.",
                nodeId = _store.NodeId
            });

        // Применяем задержку если установлена
        if (_health.ArtificialDelayMs > 0)
            await Task.Delay(_health.ArtificialDelayMs);

        var entry = _store.Put(key, request.Value);

        // Реплицируем на реплики
        var replicationReport = await _replication.ReplicateAsync(new ReplicationMessage
        {
            Action = "PUT",
            Key = key,
            Value = request.Value,
            Version = entry.Version,
            Timestamp = entry.Timestamp,
            Origin = _store.NodeId
        });

        return Ok(new
        {
            status = "written",
            entry,
            replication = replicationReport
        });
    }

    /// <summary>
    /// Прочитать данные (доступно на любом узле)
    /// </summary>
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        if (!_health.IsHealthy)
            return StatusCode(503, new { error = "Node is offline", nodeId = _store.NodeId });

        if (_health.ArtificialDelayMs > 0)
            await Task.Delay(_health.ArtificialDelayMs);

        var entry = _store.Get(key);
        if (entry == null)
            return NotFound(new { key, nodeId = _store.NodeId, message = "Key not found" });

        return Ok(new
        {
            nodeId = _store.NodeId,
            isMaster = _store.IsMaster,
            entry
        });
    }

    /// <summary>
    /// Получить все данные узла
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!_health.IsHealthy)
            return StatusCode(503, new { error = "Node is offline", nodeId = _store.NodeId });

        if (_health.ArtificialDelayMs > 0)
            await Task.Delay(_health.ArtificialDelayMs);

        return Ok(new
        {
            nodeId = _store.NodeId,
            isMaster = _store.IsMaster,
            count = _store.Count,
            entries = _store.GetAll()
        });
    }

    /// <summary>
    /// Удалить данные (только мастер)
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        if (!_health.IsHealthy)
            return StatusCode(503, new { error = "Node is offline", nodeId = _store.NodeId });

        if (!_store.IsMaster)
            return StatusCode(403, new { error = "Writes only on master", nodeId = _store.NodeId });

        if (_health.ArtificialDelayMs > 0)
            await Task.Delay(_health.ArtificialDelayMs);

        var deleted = _store.Delete(key);

        var replicationReport = await _replication.ReplicateAsync(new ReplicationMessage
        {
            Action = "DELETE",
            Key = key,
            Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Timestamp = DateTime.UtcNow,
            Origin = _store.NodeId
        });

        return Ok(new { deleted, key, replication = replicationReport });
    }
}

public class ValueRequest
{
    public string Value { get; set; } = string.Empty;
}