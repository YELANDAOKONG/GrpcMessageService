namespace MessageService.Utils;

public class TimeUtils
{
    
    public static long GetCurrentTimestampTicks()
    {
        return DateTime.UtcNow.Ticks;
    }
    
    public static long GetCurrentTimestampInMilliseconds()
    {
        return (DateTime.UtcNow - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerMillisecond;
    }
}
