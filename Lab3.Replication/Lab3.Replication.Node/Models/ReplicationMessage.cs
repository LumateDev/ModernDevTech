namespace Lab3.Replication.Models;

public class ReplicationMessage
{
    public string Action { get; set; } = "PUT"; // PUT or DELETE
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTime Timestamp { get; set; }
    public string Origin { get; set; } = string.Empty;
}

public class BulkSyncResponse
{
    public List<DataEntry> Entries { get; set; } = new();
    public string NodeId { get; set; } = string.Empty;
}