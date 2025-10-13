using Microsoft.AspNetCore.Mvc;

namespace FileEncryptor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Pong! API działa");
        }
    }
}
