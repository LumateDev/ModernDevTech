using Lab2.IndexesTransactions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lab2.IndexesTransactions.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
    private readonly TransactionService _service;

    public TransactionController(TransactionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Инициализировать счета
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        await _service.SeedAccountsAsync();
        return Ok(new { message = "Accounts seeded: id=1 balance=1000, id=2 balance=1000" });
    }

    /// <summary>
    /// Перевод денег (успешный)
    /// </summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(
        [FromQuery] int from = 1,
        [FromQuery] int to = 2,
        [FromQuery] decimal amount = 200)
    {
        var result = await _service.TransferAsync(from, to, amount, simulateError: false);
        return Ok(result);
    }

    /// <summary>
    /// Перевод денег с ошибкой (демонстрация Atomicity)
    /// </summary>
    [HttpPost("transfer-fail")]
    public async Task<IActionResult> TransferFail(
        [FromQuery] int from = 1,
        [FromQuery] int to = 2,
        [FromQuery] decimal amount = 200)
    {
        var result = await _service.TransferAsync(from, to, amount, simulateError: true);
        return Ok(result);
    }

    /// <summary>
    /// Текущие балансы
    /// </summary>
    [HttpGet("balances")]
    public async Task<IActionResult> Balances()
    {
        var result = await _service.GetBalancesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Сброс балансов
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await _service.ResetAccountsAsync();
        return Ok(new { message = "Balances reset to 1000.00" });
    }
}