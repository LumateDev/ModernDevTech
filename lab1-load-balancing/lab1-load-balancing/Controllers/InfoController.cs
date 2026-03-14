using lab1_load_balancing.Models;
using Microsoft.AspNetCore.Mvc;

namespace lab1_load_balancing.Controllers
{
    [ApiController]
    public class InfoController : ControllerBase
    {
        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "unknown";

            var response = new InfoResponse
            {
                Service = serviceName,
                Time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            return Ok(response);
        }
    }
}
