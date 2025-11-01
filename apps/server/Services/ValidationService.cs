using Cryptex.Exceptions;
using Cryptex.Models;
using Cryptex.Config;
using Microsoft.Extensions.Options;

namespace Cryptex.Services{

  public class ValidationService
  {

    private readonly AppSettings _settings;

    public ValidationService(IOptions<AppSettings> settings)
    {
      _settings = settings.Value;
    }

    public bool ValidateEncrypt(EncryptRequest request)
    {

      if (string.IsNullOrWhiteSpace(request.Password))
        throw new InvalidPasswordException("Błędne hasło.");

      if (request.File == null)
        throw new CorruptedFileException("Brak pliku.");

      if (request.Password?.Length < _settings.MIN_PASSWORD_LENGTH)
        throw new InvalidPasswordException("Hasło musi mieć co najmniej 8 znaków.");

      return true;

    }

    public bool ValidateDecrypt(DecryptRequest request)
    {

      if (string.IsNullOrWhiteSpace(request.Password))
        throw new InvalidPasswordException("Brak hasła.");

      if (request.File == null)
        throw new CorruptedFileException("Brak pliku.");

      if (Path.GetExtension(request.File.FileName) != ".enc")
        throw new CorruptedFileException("Nieprawidłowy format pliku. Oczekiwano pliku z rozszerzeniem .enc");

      return true;

    }

  }
}