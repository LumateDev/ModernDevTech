using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lab3.Replication.Tests.Models;

public class DataResponse
{
  [JsonPropertyName("nodeId")]
  public string NodeId { get; set; } = "";

  [JsonPropertyName("isMaster")]
  public bool IsMaster { get; set; }

  [JsonPropertyName("entry")]
  public EntryDto? Entry { get; set; }
}

public class EntryDto
{
  [JsonPropertyName("key")]
  public string Key { get; set; } = "";

  [JsonPropertyName("value")]
  public string Value { get; set; } = "";

  [JsonPropertyName("version")]
  public long Version { get; set; }
}

public class WriteResponse
{
  [JsonPropertyName("status")]
  public string Status { get; set; } = "";

  [JsonPropertyName("entry")]
  public EntryDto? Entry { get; set; }

  [JsonPropertyName("replication")]
  public ReplicationReportDto? Replication { get; set; }
}

public class ReplicationReportDto
{
  [JsonPropertyName("totalReplicas")]
  public int TotalReplicas { get; set; }

  [JsonPropertyName("successCount")]
  public int SuccessCount { get; set; }
}

public class AllDataResponse
{
  [JsonPropertyName("nodeId")]
  public string NodeId { get; set; } = "";

  [JsonPropertyName("count")]
  public int Count { get; set; }

  [JsonPropertyName("entries")]
  public List<EntryDto> Entries { get; set; } = new();
}

public class StatusResponse
{
  [JsonPropertyName("nodeId")]
  public string NodeId { get; set; } = "";

  [JsonPropertyName("isMaster")]
  public bool IsMaster { get; set; }

  [JsonPropertyName("isHealthy")]
  public bool IsHealthy { get; set; }

  [JsonPropertyName("entryCount")]
  public int EntryCount { get; set; }
}