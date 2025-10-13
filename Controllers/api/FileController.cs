using Cryptex.Models;
using Cryptex.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cryptex.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly EncryptionService _encService;
        public FileController(EncryptionService encService)
        {
            _encService = encService;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Pong");
        }

        [HttpPost("encrypt")]
        public async Task<IActionResult> Encrypt([FromForm] FileRequest request)
        {
            if(request.File == null || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Brak pliku lub hasła.");

            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            var data = ms.ToArray();

            var cipher = _encService.Encrypt(data, request.Password, out var iv, out var salt);

            var result = salt.Concat(iv).Concat(cipher).ToArray();
            return File(result, "application/octet-stream", request.File.FileName + ".enc");
        }

        [HttpPost("decrypt")]
        public async Task<IActionResult> Decrypt([FromForm] FileRequest request)
        {
            if(request.File == null || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Brak pliku lub hasła.");

            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            var allBytes = ms.ToArray();

            var salt = allBytes[..16];
            var iv = allBytes[16..28];
            var cipher = allBytes[28..];

            try
            {
                var plain = _encService.Decrypt(cipher, request.Password, iv, salt);
                var originalName = request.File.FileName.Replace(".enc","");
                return File(plain, "application/octet-stream", originalName);
            }
            catch
            {
                return BadRequest("Nie udało się odszyfrować. Sprawdź hasło lub plik.");
            }
        }
    }
}
