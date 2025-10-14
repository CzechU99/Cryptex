namespace Cryptex.Models
{
    public class EncryptRequest
    {
        public IFormFile File { get; set; } = default!;
        public string Password { get; set; } = string.Empty;
        public string Algorithm { get; set; } = "AES-GCM";
    }

    public class DecryptRequest
    {
        public IFormFile File { get; set; } = default!;
        public string Password { get; set; } = string.Empty;
    }
}
