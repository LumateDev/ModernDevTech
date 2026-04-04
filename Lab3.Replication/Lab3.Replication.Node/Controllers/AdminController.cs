using Lab3.Replication.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab3.Replication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController(InMemoryStore store, NodeHealthService health, ReplicationService replication) : ControllerBase
{
    private readonly InMemoryStore _store = store;
    private readonly NodeHealthService _health = health;
    private readonly ReplicationService _replication = replication;

    /// <summary>
    /// Информация об узле
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            nodeId = _store.NodeId,
            isMaster = _store.IsMaster,
            isHealthy = _health.IsHealthy,
            artificialDelayMs = _health.ArtificialDelayMs,
            entryCount = _store.Count,
            replicas = _store.IsMaster ? _replication.GetReplicaUrls() : new List<string>()
        });
    }

    /// <summary>
    /// Имитация сбоя — выключить узел
    /// </summary>
    [HttpPost("take-offline")]
    public IActionResult TakeOffline()
    {
        _health.TakeOffline();
        return Ok(new
        {
            nodeId = _store.NodeId,
            action = "taken_offline",
            isHealthy = _health.IsHealthy,
            message = "Все запросы теперь будут возвращать 503 Service Unavailable"
        });
    }

    /// <summary>
    /// Восстановление — включить узел обратно
    /// </summary>
    [HttpPost("bring-online")]
    public IActionResult BringOnline()
    {
        _health.BringOnline();
        return Ok(new
        {
            nodeId = _store.NodeId,
            action = "brought_online",
            isHealthy = _health.IsHealthy,
            message = "Узел снова доступен. Не забудьте синхронизировать данные!"
        });
    }

    /// <summary>
    /// Установить искусственную задержку (имитация сетевого лага)
    /// </summary>
    [HttpPost("set-delay")]
    public IActionResult SetDelay([FromQuery] int ms = 0)
    {
        _health.SetDelay(ms);
        return Ok(new
        {
            nodeId = _store.NodeId,
            artificialDelayMs = _health.ArtificialDelayMs,
            message = ms > 0
                ? $"Каждый запрос будет задерживаться на {ms} мс"
                : "Задержка убрана"
        });
    }

    /// <summary>
    /// Очистить все данные узла
    /// </summary>
    [HttpPost("clear")]
    public IActionResult Clear()
    {
        _store.Clear();
        return Ok(new
        {
            nodeId = _store.NodeId,
            action = "cleared",
            entryCount = _store.Count
        });
    }
}