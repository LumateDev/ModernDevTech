using Lab2.IndexesTransactions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab2.IndexesTransactions.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexDemoController : ControllerBase
{
    private readonly IndexDemoService _service;

    public IndexDemoController(IndexDemoService service)
    {
        _service = service;
    }

    /// <summary>
    /// Наполнить таблицу users 10 000 записями
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        await _service.SeedUsersAsync();
        return Ok(new { message = "Seeded 10,000 users" });
    }

    /// <summary>
    /// Поиск без индекса (Seq Scan)
    /// </summary>
    [HttpGet("search-no-index")]
    public async Task<IActionResult> SearchNoIndex([FromQuery] string email = "user_9999@example.com")
    {
        var result = await _service.SearchWithoutIndexAsync(email);
        return Ok(result);
    }

    /// <summary>
    /// Поиск с индексом (Index Scan)
    /// </summary>
    [HttpGet("search-with-index")]
    public async Task<IActionResult> SearchWithIndex([FromQuery] string email = "user_9999@example.com")
    {
        var result = await _service.SearchWithIndexAsync(email);
        return Ok(result);
    }
}