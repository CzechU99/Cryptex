using Cryptex.Models;
using Cryptex.Services;
using Microsoft.AspNetCore.Mvc;
using Cryptex.Exceptions;

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

        [HttpPost("encrypt")]
        public async Task<IActionResult> Encrypt([FromForm] FileRequest request)
        {
            if (request.File == null || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Brak pliku lub hasła.");

            if (request.Password.Length < 8)
                return BadRequest("Hasło musi mieć co najmniej 8 znaków.");

            try
            {
                using var ms = new MemoryStream();
                await request.File.CopyToAsync(ms);
                var data = ms.ToArray();

                var cipher = _encService.Encrypt(data, request.Password, out var iv, out var salt, out var passwordHash);

                var result = salt.Concat(iv).Concat(cipher).Concat(passwordHash).ToArray();
                
                return File(result, "application/octet-stream", request.File.FileName + ".enc");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas szyfrowania: {ex.Message}");
            }
        }

        [HttpPost("decrypt")]
        public async Task<IActionResult> Decrypt([FromForm] FileRequest request)
        {
            if (request.File == null || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Brak pliku lub hasła.");

            if (Path.GetExtension(request.File.FileName) != ".enc")
                return BadRequest("Nieprawidłowy format pliku. Oczekiwano pliku z rozszerzeniem .enc");

            try
            {
                using var ms = new MemoryStream();
                await request.File.CopyToAsync(ms);
                var allBytes = ms.ToArray();

                var salt = allBytes[..16];
                var iv = allBytes[16..28];
                var passwordHash = allBytes[^32..];
                var cipherWithTag = allBytes[28..^32];

                var plain = _encService.Decrypt(cipherWithTag, request.Password, iv, salt, passwordHash);
                var originalName = request.File.FileName.Replace(".enc", "");
                
                return File(plain, "application/octet-stream", originalName);
            }
            catch (InvalidPasswordException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (CorruptedFileException exception)
            {
                return BadRequest(exception.Message);
            }
        }
    }
}
