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
        private readonly ValidationService _validationService;
        private readonly FileService _fileService;
        private readonly ExpireTimeService _expireTimeService;
        
        public FileController(
            EncryptionService encService,
            RateLimitService rateLimitService,
            ValidationService validationService,
            FileService fileService,
            ExpireTimeService expireTimeService)
        {
            _encService = encService;
            _rateLimitService = rateLimitService;
            _validationService = validationService;
            _fileService = fileService;
            _expireTimeService = expireTimeService;
        }

        [HttpPost("encrypt")]
        public async Task<IActionResult> Encrypt([FromForm] EncryptRequest request)
        {
            try
            {
                _validationService.ValidateEncrypt(request);

                var fileBytes = await _fileService.FileToBytes(request);
                var expirationData = _expireTimeService.GetExpirationData(request.ExpireTime);

                var cipher = _encService.Encrypt(
                    fileBytes,
                    request.Password!,
                    request.Algorithm,
                    out var iv,
                    out var salt,
                    out var passwordHash,
                    out var algorithmByte
                );

                var result = _fileService.CombineEncryptedData(algorithmByte, salt, iv, cipher, passwordHash, expirationData.Bytes);

                return File(result, "application/octet-stream", $"{request.File!.FileName}.enc");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Błąd podczas szyfrowania: {ex.Message}");
            }
        }

        [HttpPost("decrypt")]
        public async Task<IActionResult> Decrypt([FromForm] DecryptRequest request)
        {

            try
            {
                _validationService.ValidateDecrypt(request);
                _rateLimitService.checkFileBlocked(request);

                var fileBytes = await _fileService.FileToBytes(request);

                _fileService.ExtractDetailsFromFile(fileBytes, out var algorithmType, out var salt, out var iv, out var passwordHash);

                _fileService.ExtractCipherWithTagFromFile(fileBytes, out var cipherWithTag, out var expirationBytes);

                var plain = _encService.Decrypt(
                    cipherWithTag,
                    request.Password!,
                    iv,
                    salt,
                    passwordHash,
                    algorithmType,
                    expirationBytes
                );

                _rateLimitService.ResetAttempts(request.File!.FileName);

                var originalFileName = _fileService.GetOriginalFileName(request);

                return File(plain, "application/octet-stream", originalFileName);
            }
            catch (ExpiredFileException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidPasswordException)
            {
                try
                {
                    _rateLimitService.HandleFailedAttempts(request);
                    return BadRequest("Nieznany błąd.");
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
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
