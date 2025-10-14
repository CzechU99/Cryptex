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
            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Brak hasła.");

            if (request.File == null)
                return BadRequest("Brak pliku.");

            if (request.Password?.Length < MIN_PASSWORD_LENGTH)
                return BadRequest("Hasło musi mieć co najmniej 8 znaków.");

            try
            {
                using var memoryStream = new MemoryStream();
                await request.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var algorithm = request.Algorithm == "ChaCha20-Poly1305" 
                    ? EncryptionAlgorithm.ChaCha20Poly1305 
                    : EncryptionAlgorithm.AesGcm;

                byte[]? expirationBytes = null;
                int expirationHours = 0;

                if (request.ExpireTime.HasValue)
                {
                    var expirationTime = request.ExpireTime.Value;
                    expirationHours = (int)(expirationTime - DateTime.UtcNow).TotalHours;

                    if (expirationHours <= 0)
                        return BadRequest("Czas ważności musi być w przyszłości.");

                    expirationBytes = BitConverter.GetBytes(expirationTime.Ticks);
                }

                var cipher = _encService.Encrypt(fileBytes, request.Password!, algorithm, expirationHours,
                    out var iv, out var salt, out var passwordHash, out var algorithmByte, out var expirationBytesOut);

                byte[] result;
                if (expirationBytes != null)
                {
                    // Struktura: algorithm (1) | salt (16) | iv (12) | expiration (8) | cipher + tag | passwordHash (32)
                    result = algorithmByte.Concat(salt).Concat(iv).Concat(expirationBytes).Concat(cipher).Concat(passwordHash).ToArray();
                }
                else
                {
                    // Struktura bez expiration: algorithm (1) | salt (16) | iv (12) | cipher + tag | passwordHash (32)
                    result = algorithmByte.Concat(salt).Concat(iv).Concat(cipher).Concat(passwordHash).ToArray();
                }

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
            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Brak hasła.");

            if (request.File == null)
                return BadRequest("Brak pliku.");

            if (Path.GetExtension(request.File.FileName) != ".enc")
                return BadRequest("Nieprawidłowy format pliku. Oczekiwano pliku z rozszerzeniem .enc");

            if (_rateLimitService.IsBlocked(request.File.FileName))
            {
                var remaining = _rateLimitService.GetRemainingBlockTime(request.File.FileName);
                return StatusCode(429, $"Zbyt wiele nieudanych prób. Spróbuj za {remaining.TotalMinutes:F0} minut.");
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await request.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var algorithmType = fileBytes[0];
                var decodeAlgorithm = (EncryptionAlgorithm)algorithmType;

                var salt = fileBytes[1..17];
                var iv = fileBytes[17..29];
                var passwordHash = fileBytes[^32..];

                byte[]? expirationBytes = null;
                byte[] cipherWithTag;

                // Jeśli plik ma strukturę ze wznowieniem (długość > 37)
                if (fileBytes.Length > 37)
                {
                    // Sprawdzamy czy to naprawdę expiration bytes czy cipher
                    // Jeśli fileBytes[29..37] zawiera sensowne Ticks, to expiration bytes
                    try
                    {
                        var potentialTicks = BitConverter.ToInt64(fileBytes, 29);
                        var potentialDate = new DateTime(potentialTicks, DateTimeKind.Utc);
                        
                        // Jeśli data jest rozsądna (ostatnie 100 lat), to expiration bytes
                        if (potentialDate > DateTime.UtcNow.AddYears(-100) && potentialDate < DateTime.UtcNow.AddYears(100))
                        {
                            expirationBytes = fileBytes[29..37];
                            cipherWithTag = fileBytes[37..^32];
                        }
                        else
                        {
                            // To nie expiration, to cipher
                            cipherWithTag = fileBytes[29..^32];
                        }
                    }
                    catch
                    {
                        // Jeśli konwersja się nie powiodła, to nie expiration bytes
                        cipherWithTag = fileBytes[29..^32];
                    }
                }
                else
                {
                    // Plik bez expiration time
                    cipherWithTag = fileBytes[29..^32];
                }

                var plain = _encService.Decrypt(cipherWithTag, request.Password, iv, salt, passwordHash, decodeAlgorithm, expirationBytes);

                _rateLimitService.ResetAttempts(request.File.FileName);

                var originalFileName = request.File.FileName.Replace(".enc", "");

                return File(plain, "application/octet-stream", originalFileName);
            }
            catch (ExpiredFileException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidPasswordException)
            {
                _rateLimitService.RecordFailedAttempt(request.File.FileName);
                var decodeAttempts = _rateLimitService.GetAttemptCount(request.File.FileName);
                var decodeRemainAttempts = Math.Max(0, _rateLimitService._maxAttempts - decodeAttempts);
                
                if (decodeRemainAttempts == 0)
                {
                    var lockoutTime = _rateLimitService.GetRemainingBlockTime(request.File.FileName);
                    return StatusCode(429, $"Zbyt wiele nieudanych prób. Spróbuj za {lockoutTime.TotalMinutes:F0} minut.");
                }
                
                return BadRequest($"Błędne hasło. Pozostało prób: {decodeRemainAttempts}");
            }
            catch (CorruptedFileException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas deszyfrowania: {ex.Message}");
            }
        }
    }
}
