namespace DiscordBot.Extensions
{
    public class TimeExtensions
    {
        public static DateTime GetTaipeiNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));
        }
    }
}
