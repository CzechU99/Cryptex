namespace Cryptex.Services
{
  public class ExpireTimeService
  {

    public (byte[]? Bytes, int Seconds) GetExpirationData(DateTime? expireTime)
    {
      if (!expireTime.HasValue)
        return (null, 0);

      var expirationSpan = expireTime.Value - DateTime.UtcNow;

      if (expirationSpan.TotalSeconds <= 0)
        throw new ArgumentException("Czas ważności musi być w przyszłości.");

      var bytes = BitConverter.GetBytes(expireTime.Value.ToUniversalTime().Ticks);

      return (bytes, (int)expirationSpan.TotalSeconds);
    }

  }
}