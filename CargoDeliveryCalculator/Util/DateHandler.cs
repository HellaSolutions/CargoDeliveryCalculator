using NodaTime;
using NodaTime.Text;

namespace CargoDeliveryCalculator.Util;

public class DateHandler
{
    private static readonly LocalDatePattern Pattern = LocalDatePattern.Iso;
    
    public static LocalDate GetLocalDate(string date)
    {
        var result = Pattern.Parse(date);
        return result.Success ? result.Value : throw new FormatException($"Invalid format for date {date}");
    }

    // yyyy-MM-ddTHH:mm:ss with optional fractional seconds, e.g. 2026-11-20T10:00:00
    public static LocalDateTime GetLocalDateTime(string dateTime)
    {
        var result = LocalDateTimePattern.ExtendedIso.Parse(dateTime);
        return result.Success ? result.Value : throw new FormatException($"Invalid format for date time {dateTime}");
    }

    // A LocalDateTime has no zone, so it is read as UTC to compare it with stored Instants
    public static Instant ToInstant(LocalDateTime dateTime) => dateTime.InUtc().ToInstant();

    // H is the total hours, so a gap over a day reads e.g. 26:05:03 rather than dropping the days
    private static readonly DurationPattern ElapsedPattern = DurationPattern.CreateWithInvariantCulture("H:mm:ss");

    public static string FormatElapsed(Duration elapsed) => ElapsedPattern.Format(elapsed);

    public static LocalDateTime Now()
    {
        var clock = SystemClock.Instance;
        var zone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
        return clock.GetCurrentInstant().InZone(zone).LocalDateTime;
    }

}