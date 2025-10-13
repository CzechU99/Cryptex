using System.Security.Cryptography;

namespace FileEncryptor.Services
{
    public class EncryptionService
    {
        public byte[] Encrypt(byte[] data, string password, out byte[] iv, out byte[] salt)
        {
            salt = RandomNumberGenerator.GetBytes(16);
            iv = RandomNumberGenerator.GetBytes(12);

            using var aes = new AesGcm(DeriveKey(password, salt));
            var cipher = new byte[data.Length];
            var tag = new byte[16];
            aes.Encrypt(iv, data, cipher, tag);
            
            return cipher.Concat(tag).ToArray();
        }

        public byte[] Decrypt(byte[] encryptedData, string password, byte[] iv, byte[] salt)
        {
            var cipher = encryptedData[..^16];
            var tag = encryptedData[^16..];
            using var aes = new AesGcm(DeriveKey(password, salt));
            var plain = new byte[cipher.Length];
            aes.Decrypt(iv, cipher, tag, plain);
            return plain;
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32); // 256-bit key
        }
    }
}
