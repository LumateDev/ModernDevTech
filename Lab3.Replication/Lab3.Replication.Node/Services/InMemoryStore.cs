using System.Collections.Concurrent;
using Lab3.Replication.Models;

namespace Lab3.Replication.Services;

public class InMemoryStore
{
    private readonly ConcurrentDictionary<string, DataEntry> _store = new();
    private long _versionCounter = 0;

    public string NodeId { get; }
    public bool IsMaster { get; }

    public InMemoryStore(string nodeId, bool isMaster)
    {
        NodeId = nodeId;
        IsMaster = isMaster;
    }

    /// <summary>
    /// Записать данные (генерирует новую версию)
    /// </summary>
    public DataEntry Put(string key, string value)
    {
        var version = Interlocked.Increment(ref _versionCounter);
        var entry = new DataEntry
        {
            Key = key,
            Value = value,
            Version = version,
            Timestamp = DateTime.UtcNow,
            Origin = NodeId
        };

        _store[key] = entry;
        return entry;
    }

    /// <summary>
    /// Применить реплицированные данные (только если версия новее)
    /// </summary>
    public bool ApplyReplicated(ReplicationMessage msg)
    {
        if (msg.Action == "DELETE")
        {
            _store.TryRemove(msg.Key, out _);
            return true;
        }

        var entry = new DataEntry
        {
            Key = msg.Key,
            Value = msg.Value,
            Version = msg.Version,
            Timestamp = msg.Timestamp,
            Origin = msg.Origin
        };

        return _store.AddOrUpdate(
            msg.Key,
            entry,
            (_, existing) => msg.Version > existing.Version ? entry : existing
        ).Version == msg.Version;
    }

    public DataEntry? Get(string key)
    {
        _store.TryGetValue(key, out var entry);
        return entry;
    }

    public List<DataEntry> GetAll()
    {
        return _store.Values.OrderBy(e => e.Key).ToList();
    }

    public bool Delete(string key)
    {
        return _store.TryRemove(key, out _);
    }

    public int Count => _store.Count;

    public void Clear()
    {
        _store.Clear();
        Interlocked.Exchange(ref _versionCounter, 0);
    }
}