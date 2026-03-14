using lab1_load_balancing.Models;
using lab1_load_balancing.Services;
using Microsoft.AspNetCore.Mvc;

namespace lab1_load_balancing.Controllers
{
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly CacheService _cacheService;

        public DataController(CacheService cacheService)
        {
            _cacheService = cacheService;
        }

        [HttpGet("data")]
        public IActionResult GetData([FromQuery] int id)
        {
            var (value, fromCache) = _cacheService.GetOrCreate(id);

            var response = new DataResponse
            {
                Id = id,
                Value = value,
                Source = fromCache ? "cache" : "generated"
            };

            return Ok(response);
        }
    }
}
