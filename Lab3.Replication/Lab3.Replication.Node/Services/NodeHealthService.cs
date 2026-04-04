namespace Lab3.Replication.Services;

/// <summary>
/// Управляет "здоровьем" узла — позволяет имитировать сбои
/// </summary>
public class NodeHealthService
{
    private volatile bool _isHealthy = true;
    private volatile int _artificialDelayMs = 0;

    /// <summary>
    /// Узел доступен?
    /// </summary>
    public bool IsHealthy => _isHealthy;

    /// <summary>
    /// Искусственная задержка (мс) для имитации сетевого лага
    /// </summary>
    public int ArtificialDelayMs => _artificialDelayMs;

    /// <summary>
    /// "Выключить" узел — все запросы будут возвращать 503
    /// </summary>
    public void TakeOffline()
    {
        _isHealthy = false;
    }

    /// <summary>
    /// "Включить" узел обратно
    /// </summary>
    public void BringOnline()
    {
        _isHealthy = true;
    }

    /// <summary>
    /// Задать искусственную задержку (имитация сетевого лага)
    /// </summary>
    public void SetDelay(int delayMs)
    {
        _artificialDelayMs = Math.Max(0, delayMs);
    }
}