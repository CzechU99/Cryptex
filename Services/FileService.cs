using Cryptex.Exceptions;
using Cryptex.Models;

namespace Cryptex.Services
{
  public class FileService
  {

    private const int HASH_SIZE = 32;
    private const int ITERATION_COUNT = 100_000;
    private const int TAG_SIZE = 16;
    private const int SALT_SIZE = 16;
    private const int IV_SIZE = 12;

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
      var salt = fileBytes[1..(SALT_SIZE + 1)];
      var iv = fileBytes[(SALT_SIZE + 1)..(IV_SIZE + 1)];
      var passwordHash = fileBytes[^HASH_SIZE..];

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
          var potentialTicks = BitConverter.ToInt64(fileBytes, 29);
          var potentialDate = new DateTime(potentialTicks, DateTimeKind.Utc);

          if (potentialDate > DateTime.UtcNow.AddYears(-100) && potentialDate < DateTime.UtcNow.AddYears(100))
          {
            expirationBytes = fileBytes[29..37];
            cipherWithTag = fileBytes[37..^32];
          }
          else
          {
            cipherWithTag = fileBytes[29..^32];
          }
        }
        catch
        {
          cipherWithTag = fileBytes[29..^32];
        }
      }
      else
      {
        cipherWithTag = fileBytes[29..^32];
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

  }
}