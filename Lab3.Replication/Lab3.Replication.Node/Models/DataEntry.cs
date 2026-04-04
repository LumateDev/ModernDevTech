namespace Lab3.Replication.Models;

public class DataEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Origin { get; set; } = string.Empty;
}