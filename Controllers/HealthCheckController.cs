using Microsoft.AspNetCore.Mvc;

namespace Chat_WebSocket_Server.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthCheckController : ControllerBase
    {
        private readonly ILogger<HealthCheckController> logger;

        public HealthCheckController(ILogger<HealthCheckController> logger)
        {
            this.logger = logger;
        }

        [HttpGet(Name = "health")]
        public string Get()
        {
            this.logger.LogInformation($"A request for the heath function was recieved successfully!");

            return "Server is up and running";
        }
    }
}