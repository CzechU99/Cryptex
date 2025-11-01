namespace Cryptex.Exceptions
{
    public class BlockedFileException : Exception
    {
        public BlockedFileException(string message) : base(message) { }
        public BlockedFileException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}