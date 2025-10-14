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
        private readonly RateLimitService _rateLimitService;
        private const int MIN_PASSWORD_LENGTH = 8;
        public FileController(EncryptionService encService, RateLimitService rateLimitService)
        {
            _encService = encService;
            _rateLimitService = rateLimitService;
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

            var identifier = request.File.FileName;

            if (request.File == null || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Brak pliku lub hasła.");

            if (Path.GetExtension(identifier) != ".enc")
                return BadRequest("Nieprawidłowy format pliku. Oczekiwano pliku z rozszerzeniem .enc");


            if (_rateLimitService.IsBlocked(identifier))
            {
                var remaining = _rateLimitService.GetRemainingBlockTime(identifier);
                return StatusCode(429, $"Zbyt wiele nieudanych prób. Spróbuj za {remaining.TotalMinutes:F0} minut.");
            }

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

                _rateLimitService.ResetAttempts(identifier);

                var originalName = identifier.Replace(".enc", "");

                return File(plain, "application/octet-stream", originalName);
            }
            catch (InvalidPasswordException)
            {
                _rateLimitService.RecordFailedAttempt(identifier);
                var attempts = _rateLimitService.GetAttemptCount(identifier);
                var remaining = Math.Max(0, _rateLimitService._maxAttempts - attempts);
                if (remaining == 0)
                {
                    var lockoutTime = _rateLimitService.GetRemainingBlockTime(identifier);
                    return StatusCode(429, $"Zbyt wiele nieudanych prób. Spróbuj za {lockoutTime.TotalMinutes:F0} minut.");
                }
                
                return BadRequest($"Błędne hasło. Pozostało prób: {_rateLimitService._maxAttempts - attempts}");
            }
            catch (CorruptedFileException exception)
            {
                return BadRequest(exception.Message);
            }
        }
    }
}
