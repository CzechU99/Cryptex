namespace Cryptex.Config
{
    public static class Costans
    {
        public const int SALT_SIZE = 16;
        public const int IV_SIZE = 12;
        public const int TAG_SIZE = 32;
        public const int HASH_SIZE = 32;
        public const int ITERATION_COUNT = 100_000;

        public const string DEFAULT_ENCRYPTION_ALGORITHM = "AES-GCM";
    }
}
