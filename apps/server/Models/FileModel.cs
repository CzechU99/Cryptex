using System.ComponentModel;

namespace Cryptex.Models
{
    public class EncryptRequest
    {
        public IFormFile? File { get; set; } = default!;

        [DefaultValue("")]
        public string? Password { get; set; } = string.Empty;

        [DefaultValue("AES-GCM")]
        public string Algorithm { get; set; } = "AES-GCM";

        public DateTime? ExpireTime { get; set; }
    }

    public class DecryptRequest
    {
        public IFormFile? File { get; set; } = default!;
        
        [DefaultValue("")]
        public string? Password { get; set; } = string.Empty;
    }
}
