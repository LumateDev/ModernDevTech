using Lab2.IndexesTransactions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab2.IndexesTransactions.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IsolationController : ControllerBase
{
    private readonly IsolationDemoService _service;

    public IsolationController(IsolationDemoService service)
    {
        _service = service;
    }

    /// <summary>
    /// READ COMMITTED — видит только закоммиченные данные
    /// </summary>
    [HttpGet("read-committed")]
    public async Task<IActionResult> ReadCommitted()
    {
        var result = await _service.DemoReadCommittedAsync();
        return Ok(result);
    }

    /// <summary>
    /// READ UNCOMMITTED (PostgreSQL приводит к READ COMMITTED)
    /// </summary>
    [HttpGet("read-uncommitted")]
    public async Task<IActionResult> ReadUncommitted()
    {
        var result = await _service.DemoReadUncommittedAsync();
        return Ok(result);
    }

    /// <summary>
    /// REPEATABLE READ — снимок на момент начала транзакции
    /// </summary>
    [HttpGet("repeatable-read")]
    public async Task<IActionResult> RepeatableRead()
    {
        var result = await _service.DemoRepeatableReadAsync();
        return Ok(result);
    }

    /// <summary>
    /// SERIALIZABLE — полная изоляция
    /// </summary>
    [HttpGet("serializable")]
    public async Task<IActionResult> Serializable()
    {
        var result = await _service.DemoSerializableAsync();
        return Ok(result);
    }
}