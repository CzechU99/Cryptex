using Cryptex.Models;
using Cryptex.Services;
using Microsoft.AspNetCore.Mvc;
using Cryptex.Exceptions;

namespace Cryptex.Controllers.api
{
    [ApiController]
    [Route("api/")]
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
                var encryptedFile = _encService.Encrypt(fileBytes, request.Password!, request.Algorithm);
                var result = _fileService.CombineEncryptedData(encryptedFile, expirationData.Bytes);

                return File(result, "application/octet-stream", $"{request.File!.FileName}.enc");
            }
            catch (Exception exception) when (exception is
                ArgumentException or
                InvalidPasswordException or
                CorruptedFileException
            )
            {
                return BadRequest(exception.Message);
            }
            catch (Exception exception)
            {
                return StatusCode(500, $"Błąd podczas szyfrowania: {exception.Message}");
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
                var plain = _encService.Decrypt(fileBytes, request.Password!);
                var originalFileName = _fileService.GetOriginalFileName(request);

                _rateLimitService.ResetAttempts(request.File!.FileName);
                
                return File(plain, "application/octet-stream", originalFileName);
            }
            catch (Exception exception) when (exception is
                ExpiredFileException or
                CorruptedFileException or
                NotSupportedException or
                BlockedFileException)
            {
                return BadRequest(exception.Message);
            }
            catch (InvalidPasswordException)
            {
                try
                {
                    _rateLimitService.HandleFailedAttempts(request);
                    return BadRequest("Nieznany błąd.");
                }
                catch (BlockedFileException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            catch (Exception exception)
            {
                return StatusCode(500, $"Błąd podczas deszyfrowania: {exception.Message}");
            }
        }
    }
}
