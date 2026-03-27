using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lab2.IndexesTransactions.Tests.Models;

public record SearchResult(
    [property: JsonPropertyName("elapsed_ms")] double ElapsedMs,
    [property: JsonPropertyName("explain")] List<string> Explain);

public record AccountDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("balance")] decimal Balance);

public record CacheResponse(
    [property: JsonPropertyName("source")] string Source);

public record IsolationResult(
    [property: JsonPropertyName("explanation")] string Explanation);