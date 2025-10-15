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
    
    public void extractDetailsFromFile(byte[] fileBytes, out byte algorithmType, out byte[] salt, out byte[] iv, out byte[] passwordHash)
    {

      algorithmType = fileBytes[0];
      salt = fileBytes[1..17];
      iv = fileBytes[17..29];
      passwordHash = fileBytes[^32..];

    }

  }
}