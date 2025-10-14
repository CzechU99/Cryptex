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
        private const int MIN_PASSWORD_LENGTH = 8;
        public FileController(EncryptionService encService)
        {
            _encService = encService;
        }

        [HttpPost("encrypt")]
        public async Task<IActionResult> Encrypt([FromForm] EncryptRequest request)
        {
            if (request.File == null || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Brak pliku lub hasła.");

            if (request.Password.Length < MIN_PASSWORD_LENGTH)
                return BadRequest("Hasło musi mieć co najmniej 8 znaków.");

            try
            {
                using var ms = new MemoryStream();
                await request.File.CopyToAsync(ms);
                var data = ms.ToArray();

                var algorithm = request.Algorithm == "ChaCha20-Poly1305" ? EncryptionAlgorithm.ChaCha20Poly1305 : EncryptionAlgorithm.AesGcm;

                var cipher = _encService.Encrypt(data, request.Password, algorithm, out var iv, out var salt, out var passwordHash, out var algorithmByte);

                var result = algorithmByte.Concat(salt).Concat(iv).Concat(cipher).Concat(passwordHash).ToArray();
                
                return File(result, "application/octet-stream", request.File.FileName + ".enc");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas szyfrowania: {ex.Message}");
            }
        }

        [HttpPost("decrypt")]
        public async Task<IActionResult> Decrypt([FromForm] DecryptRequest request)
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

                var algorithmByte = allBytes[0];
                var algorithm = (EncryptionAlgorithm)algorithmByte;

                var salt = allBytes[1..17];
                var iv = allBytes[17..29];
                var passwordHash = allBytes[^32..];
                var cipherWithTag = allBytes[29..^32];

                var plain = _encService.Decrypt(cipherWithTag, request.Password, iv, salt, passwordHash, algorithm);
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
