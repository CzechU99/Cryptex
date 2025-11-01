using System.Security.Cryptography;
using Cryptex.Exceptions;
using Cryptex.Config;
using Microsoft.Extensions.Options;

namespace Cryptex.Services
{
    public enum EncryptionAlgorithm
    {
        AesGcm = 0,
        ChaCha20Poly1305 = 1
    }

    public class EncryptionService
    {
        private readonly FileService _fileService;
        private readonly AppSettings _settings;

        public EncryptionService(FileService fileService, IOptions<AppSettings> settings)
        {
            _fileService = fileService;
            _settings = settings.Value;
        }

        public byte[] Encrypt(byte[] data, string password, string algorithm)
        {
            
            var (salt, iv, passwordHash, encAlgorithm) = InitializeEncryptionParameters(password, algorithm);
            var key = DeriveKey(password, salt);

            switch (encAlgorithm)
            {
                case EncryptionAlgorithm.AesGcm:
                    return EncryptAesGcm(data, key, iv, salt, passwordHash, encAlgorithm);
                case EncryptionAlgorithm.ChaCha20Poly1305:
                    return EncryptChaCha20Poly1305(data, key, iv, salt, passwordHash, encAlgorithm);
                default:
                    throw new ArgumentException("Nieznany algorytm szyfrowania");
            }

        }

        public byte[] Decrypt(byte[] fileBytes, string password)
        {

            var (algorithmType, salt, iv, passwordHash) = _fileService.ExtractDetailsFromFile(fileBytes);
            var (cipherWithTag, expirationBytes) = _fileService.ExtractCipherTagAndDate(fileBytes);
            
            CheckPassword(password, salt, passwordHash);

            _fileService.CheckFileExpiration(expirationBytes);

            var (cipher, tag, plain) = SplitEncryptedData(cipherWithTag);
            var key = DeriveKey(password, salt);

            try
            {
                DecryptWithAlgorithm(algorithmType, key, iv, cipher, tag, plain);
            }
            catch (AuthenticationTagMismatchException)
            {
                throw new CorruptedFileException("Plik jest uszkodzony. Sprawdź integralność danych.");
            }

            return plain;
        }

        private byte[] ComputePasswordHash(string password, byte[] salt)
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            using var kdf = new Rfc2898DeriveBytes(passwordBytes, salt, _settings.ITERATION_COUNT, HashAlgorithmName.SHA256);
            return kdf.GetBytes(_settings.HASH_SIZE);
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            using var kdf = new Rfc2898DeriveBytes(passwordBytes, salt, _settings.ITERATION_COUNT, HashAlgorithmName.SHA256);
            return kdf.GetBytes(_settings.HASH_SIZE);
        }
        private byte[] EncryptAesGcm(byte[] data, byte[] key, byte[] iv, byte[] salt, byte[] passwordHash, EncryptionAlgorithm algorithmByte)
        {
            using var aes = new AesGcm(key, _settings.TAG_SIZE);
            var cipher = new byte[data.Length];
            var tag = new byte[_settings.TAG_SIZE];
            aes.Encrypt(iv, data, cipher, tag);

            var algorithmByteArray = new[] { (byte)algorithmByte };

            return algorithmByteArray.Concat(salt).Concat(iv).Concat(cipher).Concat(tag).Concat(passwordHash).ToArray();
        }

        private byte[] EncryptChaCha20Poly1305(byte[] data, byte[] key, byte[] iv, byte[] salt, byte[] passwordHash, EncryptionAlgorithm algorithmByte)
        {
            using var chacha = new ChaCha20Poly1305(key);
            var cipher = new byte[data.Length];
            var tag = new byte[_settings.TAG_SIZE];
            chacha.Encrypt(iv, data, cipher, tag);

            var algorithmByteArray = new[] { (byte)algorithmByte };

            return algorithmByteArray.Concat(salt).Concat(iv).Concat(cipher).Concat(tag).Concat(passwordHash).ToArray();
        }

        private void CheckPassword(string password, byte[] salt, byte[] passwordHash)
        {
            var computedHash = ComputePasswordHash(password, salt);
            if (!computedHash.SequenceEqual(passwordHash))
            {
                throw new InvalidPasswordException("Błędne hasło.");
            }
        }

        private static (byte[] cipher, byte[] tag, byte[] plain) SplitEncryptedData(byte[] encryptedData)
        {
            var cipher = encryptedData[..^16];
            var tag = encryptedData[^16..];
            var plain = new byte[cipher.Length];

            return (cipher, tag, plain);
        }

        private (byte[] salt, byte[] iv, byte[] passwordHash, EncryptionAlgorithm algorithmByte) InitializeEncryptionParameters(
            string password, string algorithm)
        {
            var salt = RandomNumberGenerator.GetBytes(_settings.SALT_SIZE);
            var iv = RandomNumberGenerator.GetBytes(_settings.IV_SIZE);
            var passwordHash = ComputePasswordHash(password, salt);

            if(algorithm != "AES-GCM" && algorithm != "ChaCha20-Poly1305")
                throw new ArgumentException("Nieznany algorytm szyfrowania");

            var encAlgorithm = algorithm == "ChaCha20-Poly1305"
                ? EncryptionAlgorithm.ChaCha20Poly1305
                : EncryptionAlgorithm.AesGcm;

            var algorithmByte = encAlgorithm;

            return (salt, iv, passwordHash, algorithmByte);
        }
        
        private void DecryptWithAlgorithm(byte algorithm, byte[] key, byte[] iv, byte[] cipher, byte[] tag, byte[] plain)
        {
            var decodeAlgorithm = (EncryptionAlgorithm)algorithm;

            switch (decodeAlgorithm)
            {
                case EncryptionAlgorithm.AesGcm:
                    var aes = new AesGcm(key, _settings.TAG_SIZE);
                    aes.Decrypt(iv, cipher, tag, plain);
                    break;

                case EncryptionAlgorithm.ChaCha20Poly1305:
                    using (var chacha = new ChaCha20Poly1305(key))
                    {
                        chacha.Decrypt(iv, cipher, tag, plain);
                    }
                    break;

                default:
                    throw new NotSupportedException($"Nieobsługiwany algorytm: {algorithm}");
            }
        }
        
    }
}