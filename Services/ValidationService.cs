using Cryptex.Models;
namespace Cryptex.Services
{
  public class ValidationService
  {

    private readonly int MIN_PASSWORD_LENGTH = 8;

    public bool ValidateEncrypt(EncryptRequest request)
    {

      if (string.IsNullOrWhiteSpace(request.Password))
        throw new Exception("Błędne hasło.");

      if (request.File == null)
        throw new Exception("Brak pliku.");

      if (request.Password?.Length < MIN_PASSWORD_LENGTH)
        throw new Exception("Hasło musi mieć co najmniej 8 znaków.");

      return true;

    }

    public bool ValidateDecrypt(DecryptRequest request)
    {

      if (string.IsNullOrWhiteSpace(request.Password))
        throw new Exception("Brak hasła.");

      if (request.File == null)
        throw new Exception("Brak pliku.");

      if (Path.GetExtension(request.File.FileName) != ".enc")
        throw new Exception("Nieprawidłowy format pliku. Oczekiwano pliku z rozszerzeniem .enc");

      return true;

    }

  }
}