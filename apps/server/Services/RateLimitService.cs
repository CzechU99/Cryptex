using System.Collections.Concurrent;
using Cryptex.Models;
using Cryptex.Exceptions;
using Cryptex.Config;
using Microsoft.Extensions.Options;

namespace Cryptex.Services
{
  public class RateLimitService
  {

    private class AttemptRecord
    {
      public int FailedAttempts { get; set; }
      public DateTime BlockedUntil { get; set; }
    }

    private readonly ConcurrentDictionary<string, AttemptRecord> _attempts;
    public readonly int _maxAttempts;
    private readonly int _lockoutDurationMinutes;
    private readonly Timer _cleanupTimer;

    public RateLimitService(IOptions<AppSettings> settings)
    {
      var _settings = settings.Value;
      _attempts = new ConcurrentDictionary<string, AttemptRecord>();
      _maxAttempts = _settings.MAX_ATTEMPTS;
      _lockoutDurationMinutes = _settings.LOCKOUT_DURATION_MINUTES;

      _cleanupTimer = new Timer(CleanupExpiredRecords, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    public bool IsBlocked(string identifier)
    {
      if (_attempts.TryGetValue(identifier, out var record))
      {
        if (DateTime.UtcNow < record.BlockedUntil)
        {
          return true;
        }
      }
      return false;
    }

    public bool checkFileBlocked(DecryptRequest request)
    {
      if (IsBlocked(request.File!.FileName))
      {
        var remaining = GetRemainingBlockTime(request.File.FileName);
        throw new BlockedFileException($"Zbyt wiele nieudanych prób. Spróbuj za {remaining.TotalMinutes:F0} minut.");
      }
      return false;
    }

    public TimeSpan GetRemainingBlockTime(string identifier)
    {
      if (_attempts.TryGetValue(identifier, out var record))
      {
        var remaining = record.BlockedUntil - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
      }
      return TimeSpan.Zero;
    }

    public void RecordFailedAttempt(string identifier)
    {
      _attempts.AddOrUpdate(identifier,
      new AttemptRecord 
      { 
        FailedAttempts = 1,
        BlockedUntil = DateTime.UtcNow
      },
      (key, existing) =>
      {
        existing.FailedAttempts++;
        if (existing.FailedAttempts >= _maxAttempts)
        {
          existing.BlockedUntil = DateTime.UtcNow.AddMinutes(_lockoutDurationMinutes);
        }
        return existing;
      });
    }

    public void ResetAttempts(string identifier)
    {
      _attempts.TryRemove(identifier, out _);
    }

    public int GetAttemptCount(string identifier)
    {
      if (_attempts.TryGetValue(identifier, out var record))
      {
        return record.FailedAttempts;
      }
      return 0;
    }

    private void CleanupExpiredRecords(object? state)
    {
      var now = DateTime.UtcNow;
      var expiredKeys = _attempts
        .Where(x => now > x.Value.BlockedUntil.AddMinutes(_lockoutDurationMinutes))
        .Select(x => x.Key)
        .ToList();

      foreach (var key in expiredKeys)
      {
        _attempts.TryRemove(key, out _);
      }
    }

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
    
    public void HandleFailedAttempts(DecryptRequest request)
    {

      RecordFailedAttempt(request.File!.FileName);
      var decodeAttempts = GetAttemptCount(request.File.FileName);
      var decodeRemainAttempts = Math.Max(0, _maxAttempts - decodeAttempts);
      
      if (decodeRemainAttempts == 0)
      {
        var lockoutTime = GetRemainingBlockTime(request.File.FileName);
        throw new BlockedFileException($"Zbyt wiele nieudanych prób. Spróbuj za {lockoutTime.TotalMinutes:F0} minut.");  
      }

      throw new BlockedFileException($"Błędne hasło. Pozostało prób: {decodeRemainAttempts}");

    }

  }
}