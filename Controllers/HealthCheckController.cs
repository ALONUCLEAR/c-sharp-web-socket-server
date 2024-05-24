using Microsoft.AspNetCore.Mvc;

namespace Chat_WebSocket_Server.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthCheckController : ControllerBase
    {

        private readonly ILogger<HealthCheckController> _logger;

        public HealthCheckController(ILogger<HealthCheckController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "health")]
        public string Get()
        {
            return "Server is up and running";
        }
    }
}