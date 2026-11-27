namespace MotionLink.Models;

public class SessionOverview
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public int SwingCount { get; set; } = 0;
    public double? AverageTempoRatio { get; set; }

    public string DurationDisplay
    {
        get
        {
            if (DateEnd is not DateTime end) return "Not ended";

            var duration = end - DateStart;
            if (duration < TimeSpan.Zero) return "Unavailable";

            return duration.TotalHours >= 1
                ? $"{(long)duration.TotalHours}h {duration.Minutes:D2}m {duration.Seconds:D2}s"
                : $"{duration.Minutes}m {duration.Seconds:D2}s";
        }
    }
}
