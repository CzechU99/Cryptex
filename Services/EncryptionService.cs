using System.Security.Cryptography;
using Cryptex.Exceptions;
using Cryptex.Models;

namespace Cryptex.Services
{
    public enum EncryptionAlgorithm
    {
        AesGcm = 0,
        ChaCha20Poly1305 = 1
    }

    public class EncryptionService
    {
        private const int HASH_SIZE = 32;
        private const int ITERATION_COUNT = 100_000;
        private const int TAG_SIZE = 16;
        private const int SALT_SIZE = 16;
        private const int IV_SIZE = 12;

        public byte[] Encrypt(byte[] data, string password, string algorithm, out byte[] iv, out byte[] salt,
            out byte[] passwordHash, out byte[] algorithmByte)
        {
            salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
            iv = RandomNumberGenerator.GetBytes(IV_SIZE);
            passwordHash = ComputePasswordHash(password, salt);
            
            var encAlgorithm = algorithm == "ChaCha20-Poly1305"
                ? EncryptionAlgorithm.ChaCha20Poly1305
                : EncryptionAlgorithm.AesGcm;
            
            
            algorithmByte = new[] { (byte)encAlgorithm };

            var key = DeriveKey(password, salt);

            return encAlgorithm switch
            {
                EncryptionAlgorithm.AesGcm => EncryptAesGcm(data, key, iv),
                EncryptionAlgorithm.ChaCha20Poly1305 => EncryptChaCha20Poly1305(data, key, iv),
                _ => throw new ArgumentException("Nieznany algorytm szyfrowania")
            };
        }

        public byte[] Decrypt(byte[] encryptedData, string password, byte[] iv, byte[] salt,
            byte[] passwordHash, byte algorithm, byte[]? expireBytes)
        {

            CheckPassword(password, salt, passwordHash);

            var decodeAlgorithm = (EncryptionAlgorithm)algorithm;


            if (expireBytes != null && expireBytes.Length == 8)
            {
                var expireTimeTicks = BitConverter.ToInt64(expireBytes);
                var expireTime = new DateTime(expireTimeTicks, DateTimeKind.Utc);
                
                if (DateTime.UtcNow > expireTime)
                {
                    throw new ExpiredFileException("Plik wygasł i nie może być odszyfrowany.");
                }
            }

            var (cipher, tag, plain) = SplitEncryptedData(encryptedData);
            var key = DeriveKey(password, salt);

            try
            {
                switch (decodeAlgorithm)
                {
                    case EncryptionAlgorithm.AesGcm:
                        using (var aes = new AesGcm(key, TAG_SIZE))
                        {
                            aes.Decrypt(iv, cipher, tag, plain);
                        }
                        break;

                    case EncryptionAlgorithm.ChaCha20Poly1305:
                        using (var chacha = new ChaCha20Poly1305(key))
                        {
                            chacha.Decrypt(iv, cipher, tag, plain);
                        }
                        break;
                }
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
            return kdf.GetBytes(HASH_SIZE);
        }

        private static byte[] EncryptAesGcm(byte[] data, byte[] key, byte[] iv)
        {
            using var aes = new AesGcm(key, TAG_SIZE);
            var cipher = new byte[data.Length];
            var tag = new byte[TAG_SIZE];
            aes.Encrypt(iv, data, cipher, tag);

            return cipher.Concat(tag).ToArray();
        }

        private static byte[] EncryptChaCha20Poly1305(byte[] data, byte[] key, byte[] iv)
        {
            using var chacha = new ChaCha20Poly1305(key);
            var cipher = new byte[data.Length];
            var tag = new byte[TAG_SIZE];
            chacha.Encrypt(iv, data, cipher, tag);

            return cipher.Concat(tag).ToArray();
        }

        private static void CheckPassword(string password, byte[] salt, byte[] passwordHash)
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
    }
}