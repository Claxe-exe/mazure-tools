using System.Globalization;

namespace MazureTools.Core;

public static class ByteFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string Format(long bytes) => Format((double)bytes);

    public static string Format(double bytes)
    {
        if (bytes < 0)
            bytes = 0;

        var unit = 0;
        while (bytes >= 1024 && unit < Units.Length - 1)
        {
            bytes /= 1024;
            unit++;
        }

        var format = unit == 0 ? "0" : bytes >= 100 ? "0" : bytes >= 10 ? "0.0" : "0.00";
        return $"{bytes.ToString(format, CultureInfo.CurrentCulture)} {Units[unit]}";
    }

    public static string FormatDuration(TimeSpan span) =>
        span.TotalDays >= 1
            ? Loc.T("Time_DaysFormat", (int)span.TotalDays, span.Hours, span.Minutes, span.Seconds)
            : Loc.T("Time_HoursFormat", span.Hours, span.Minutes, span.Seconds);
}
