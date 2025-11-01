namespace Cryptex.Config
{
    public class AppSettings
    {
        public int SALT_SIZE { get; set; }
        public int IV_SIZE { get; set; }
        public int TAG_SIZE { get; set; }
        public int MAX_ATTEMPTS { get; set; }
        public int LOCKOUT_DURATION_MINUTES { get; set; }
        public int HASH_SIZE { get; set; }
        public int ITERATION_COUNT { get; set; }
        public int MIN_PASSWORD_LENGTH { get; set; }
        public string? DEFAULT_ENCRYPTION_ALGORITHM { get; set; }
    }
}
