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

            var fileToDecrypt = request.File;
            var fileIdentifier = fileToDecrypt.FileName;
            var passwordToDecrypt = request.Password;

            var isPassword = !string.IsNullOrWhiteSpace(passwordToDecrypt);
            if (!isPassword)
                return BadRequest("Brak hasła.");

            var isFile = fileToDecrypt != null;
            if (!isFile)
                return BadRequest("Brak pliku.");

            if (passwordToDecrypt.Length < MIN_PASSWORD_LENGTH)
                return BadRequest("Hasło musi mieć co najmniej 8 znaków.");

            try
            {
                using var memoryStream = new MemoryStream();
                await fileToDecrypt.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var algorithm = request.Algorithm == "ChaCha20-Poly1305" ? EncryptionAlgorithm.ChaCha20Poly1305 : EncryptionAlgorithm.AesGcm;

                var cipher = _encService.Encrypt(fileBytes, passwordToDecrypt, algorithm, out var iv, out var salt, out var passwordHash, out var algorithmByte);

                var result = algorithmByte.Concat(salt).Concat(iv).Concat(cipher).Concat(passwordHash).ToArray();
                
                return File(result, "application/octet-stream", fileIdentifier + ".enc");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas szyfrowania: {ex.Message}");
            }
        }

        [HttpPost("decrypt")]
        public async Task<IActionResult> Decrypt([FromForm] DecryptRequest request)
        {

            var fileToDecrypt = request.File;
            var fileIdentifier = fileToDecrypt.FileName;
            var passwordToDecrypt = request.Password;

            var isPassword = !string.IsNullOrWhiteSpace(passwordToDecrypt);
            if (!isPassword)
                return BadRequest("Brak hasła.");

            var isFile = fileToDecrypt != null;
            if (!isFile)
                return BadRequest("Brak pliku.");

            var fileExtension = Path.GetExtension(fileIdentifier);
            if (fileExtension != ".enc")
                return BadRequest("Nieprawidłowy format pliku. Oczekiwano pliku z rozszerzeniem .enc");


            if (_rateLimitService.IsBlocked(fileIdentifier))
            {
                var remaining = _rateLimitService.GetRemainingBlockTime(fileIdentifier);
                return StatusCode(429, $"Zbyt wiele nieudanych prób. Spróbuj za {remaining.TotalMinutes:F0} minut.");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await fileToDecrypt.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var algorithmType = fileBytes[0];
                var decodeAlgorithm = (EncryptionAlgorithm)algorithmType;

                var salt = fileBytes[1..17];
                var iv = fileBytes[17..29];
                var passwordHash = fileBytes[^32..];
                var cipherWithTag = fileBytes[29..^32];

                var plain = _encService.Decrypt(cipherWithTag, passwordToDecrypt, iv, salt, passwordHash, decodeAlgorithm);

                _rateLimitService.ResetAttempts(fileIdentifier);

                var originalFileName = fileIdentifier.Replace(".enc", "");

                return File(plain, "application/octet-stream", originalFileName);
            }
            catch (InvalidPasswordException)
            {
                _rateLimitService.RecordFailedAttempt(fileIdentifier);
                var decodeAttempts = _rateLimitService.GetAttemptCount(fileIdentifier);
                var decodeRemainAttempts = Math.Max(0, _rateLimitService._maxAttempts - decodeAttempts);
                if (decodeRemainAttempts == 0)
                {
                    var lockoutTime = _rateLimitService.GetRemainingBlockTime(fileIdentifier);
                    return StatusCode(429, $"Zbyt wiele nieudanych prób. Spróbuj za {lockoutTime.TotalMinutes:F0} minut.");
                }
                
                return BadRequest($"Błędne hasło. Pozostało prób: {decodeRemainAttempts}");
            }
            catch (CorruptedFileException exception)
            {
                return BadRequest(exception.Message);
            }
        }
    }
}
