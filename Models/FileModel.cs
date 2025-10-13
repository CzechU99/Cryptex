namespace FileEncryptor.Models
{
    public class FileRequest
    {
        public IFormFile File { get; set; } = default!;
        public string Password { get; set; } = string.Empty;
    }
}
