namespace Cryptex.Exceptions
{
    public class ExpiredFileException : Exception
    {
        public ExpiredFileException(string message) : base(message) { }
        public ExpiredFileException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}