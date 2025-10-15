using Cryptex.Exceptions;
using Cryptex.Models;

namespace Cryptex.Services
{
  public class FileService
  {

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

    public byte[] CombineEncryptedData(
      byte[] algorithm, byte[] salt, byte[] iv, byte[] cipher, byte[] passwordHash, byte[]? expirationBytes = null)
    {
      var parts = new List<byte[]> { algorithm, salt, iv };

      if (expirationBytes != null)
        parts.Add(expirationBytes);

      parts.Add(cipher);
      parts.Add(passwordHash);

      return parts.SelectMany(x => x).ToArray();
    }

    public void ExtractDetailsFromFile(byte[] fileBytes, out byte algorithmType, out byte[] salt, out byte[] iv, out byte[] passwordHash)
    {

      algorithmType = fileBytes[0];
      salt = fileBytes[1..17];
      iv = fileBytes[17..29];
      passwordHash = fileBytes[^32..];

    }

    public void ExtractCipherWithTagFromFile(byte[] fileBytes, out byte[] cipherWithTag, out byte[]? expirationBytes)
    {
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
            expirationBytes = null;
          }
        }
        catch
        {
          cipherWithTag = fileBytes[29..^32];
          expirationBytes = null;
        }
      }
      else
      {
        cipherWithTag = fileBytes[29..^32];
        expirationBytes = null;
      }
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