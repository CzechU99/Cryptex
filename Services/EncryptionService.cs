using System.Security.Cryptography;
using Cryptex.Exceptions;

namespace Cryptex.Services
{
    public class EncryptionService
    {
        private const int HASH_SIZE = 32;
        private const int ITERATION_COUNT = 100_000;

        public byte[] Encrypt(byte[] data, string password, out byte[] iv, out byte[] salt, out byte[] passwordHash)
        {
            salt = RandomNumberGenerator.GetBytes(16);
            iv = RandomNumberGenerator.GetBytes(12);
            passwordHash = ComputePasswordHash(password, salt);

            using var aes = new AesGcm(DeriveKey(password, salt), tagSizeInBytes: 16);
            var cipher = new byte[data.Length];
            var tag = new byte[16];
            aes.Encrypt(iv, data, cipher, tag);

            return cipher.Concat(tag).ToArray();
        }

        public byte[] Decrypt(byte[] encryptedData, string password, byte[] iv, byte[] salt, byte[] passwordHash)
        {
            var computedHash = ComputePasswordHash(password, salt);
            if (!computedHash.SequenceEqual(passwordHash))
            {
                throw new InvalidPasswordException("Błędne hasło.");
            }

            var cipher = encryptedData[..^16];
            var tag = encryptedData[^16..];
            var plain = new byte[cipher.Length];

            using var aes = new AesGcm(DeriveKey(password, salt), tagSizeInBytes: 16);

            try
            {
                aes.Decrypt(iv, cipher, tag, plain);
            }
            catch (AuthenticationTagMismatchException)
            {
                throw new CorruptedFileException("Plik jest uszkodzony. Sprawdź integralność danych.");
            }

            return plain;
        }

        private static byte[] ComputePasswordHash(string password, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, ITERATION_COUNT, HashAlgorithmName.SHA256);
            return kdf.GetBytes(HASH_SIZE);
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, ITERATION_COUNT, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }
    }
}