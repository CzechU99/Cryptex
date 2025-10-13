namespace Cryptex.Exceptions
{
    public class CorruptedFileException : Exception
    {
        public CorruptedFileException(string message) : base(message) { }
        public CorruptedFileException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}