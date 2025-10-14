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

  }
}