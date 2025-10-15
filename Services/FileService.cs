using Cryptex.Exceptions;
using Cryptex.Models;
using Cryptex.Config;
using Microsoft.Extensions.Options;

namespace Cryptex.Services
{
  public class FileService
  {

    private readonly AppSettings _settings;

    public FileService(IOptions<AppSettings> settings)
    {
      _settings = settings.Value;
    }

    public async Task<byte[]> FileToBytes(EncryptRequest request)
    {
      using var memoryStream = new MemoryStream();
      await request.File!.CopyToAsync(memoryStream);
      var fileBytes = memoryStream.ToArray();

      return fileBytes;
    }

    public async Task<byte[]> FileToBytes(DecryptRequest request)
    {
      using var memoryStream = new MemoryStream();
      await request.File!.CopyToAsync(memoryStream);
      var fileBytes = memoryStream.ToArray();

      return fileBytes;
    }

    public byte[] CombineEncryptedData(byte[] encryptedFile, byte[]? expirationBytes = null)
    {
      if (expirationBytes != null)
        return encryptedFile.Take(29).Concat(expirationBytes).Concat(encryptedFile.Skip(29)).ToArray();
      return encryptedFile;
    }

    public (byte, byte[], byte[], byte[]) ExtractDetailsFromFile(byte[] fileBytes)
    {
      var algorithmType = fileBytes[0];
      var salt = fileBytes[1..(_settings.SALT_SIZE + 1)];
      var iv = fileBytes[(_settings.SALT_SIZE + 1)..(_settings.IV_SIZE + 1 + _settings.SALT_SIZE)];
      var passwordHash = fileBytes[^_settings.HASH_SIZE..];

      return (algorithmType, salt, iv, passwordHash);
    }

    public (byte[] cipherWithTag, byte[]? expirationBytes) ExtractCipherTagAndDate(byte[] fileBytes)
    {
      byte[] cipherWithTag;
      byte[]? expirationBytes = null;

      if (fileBytes.Length > 37)
      {
        try
        {
          var potentialTicks = BitConverter.ToInt64(fileBytes, 1 + _settings.IV_SIZE + _settings.SALT_SIZE);
          var potentialDate = new DateTime(potentialTicks, DateTimeKind.Utc);

          if (potentialDate > DateTime.UtcNow.AddYears(-100) && potentialDate < DateTime.UtcNow.AddYears(100))
          {
            expirationBytes = fileBytes[(1 + _settings.IV_SIZE + _settings.SALT_SIZE)..(1 + _settings.IV_SIZE + _settings.SALT_SIZE + 8)];
            cipherWithTag = fileBytes[(1 + _settings.IV_SIZE + _settings.SALT_SIZE + 8)..^_settings.HASH_SIZE];
          }
          else
          {
            cipherWithTag = fileBytes[(1 + _settings.IV_SIZE + _settings.SALT_SIZE)..^_settings.HASH_SIZE];
          }
        }
        catch
        {
          cipherWithTag = fileBytes[(1 + _settings.IV_SIZE + _settings.SALT_SIZE)..^_settings.HASH_SIZE];
        }
      }
      else
      {
        cipherWithTag = fileBytes[(1 + _settings.IV_SIZE + _settings.SALT_SIZE)..^_settings.HASH_SIZE];
      }

      return (cipherWithTag, expirationBytes);
    }

    public string GetOriginalFileName(DecryptRequest request)
    {
      var originalFileName = request.File!.FileName;
      if (originalFileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
      {
        originalFileName = originalFileName[..^4];
      }
      return originalFileName;
    }
    
    public void CheckFileExpiration(byte[]? expirationBytes)
    {
      if (expirationBytes != null && expirationBytes.Length == _settings.MIN_PASSWORD_LENGTH)
      {
        var expireTimeTicks = BitConverter.ToInt64(expirationBytes);
        var expireTime = new DateTime(expireTimeTicks, DateTimeKind.Utc);

        if (DateTime.UtcNow > expireTime)
        {
          throw new ExpiredFileException("Plik wygasł i nie może zostać odszyfrowany.");
        }
      }
    }

  }
}