using System.Collections.Concurrent;

namespace lab1_load_balancing.Services
{
    public class CacheService
    {
        private readonly ConcurrentDictionary<int, string> _cache = new();

        public (string value, bool fromCache) GetOrCreate(int id)
        {
            // Проверяем кэш
            if (_cache.TryGetValue(id, out var existing))
            {
                return (existing, true);
            }

            // Генерируем случайное значение
            var generated = $"Random data {Guid.NewGuid().ToString()[..8]}";
            _cache[id] = generated;
            return (generated, false);
        }
    }
}
