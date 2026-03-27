using Lab2.IndexesTransactions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab2.IndexesTransactions.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly CacheService _service;

    public CacheController(CacheService service)
    {
        _service = service;
    }

    /// <summary>
    /// Получить пользователя (Cache Aside: кэш → БД → кэш)
    /// </summary>
    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _service.GetUserAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Очистить кэш для пользователя
    /// </summary>
    [HttpDelete("user/{id}")]
    public async Task<IActionResult> ClearCache(int id)
    {
        var result = await _service.ClearCacheAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Статистика Redis
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var result = await _service.GetStatsAsync();
        return Ok(result);
    }
}