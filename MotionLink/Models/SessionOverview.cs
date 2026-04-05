namespace MotionLink.Models;

public class SessionOverview
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset DateStart { get; set; }
    public DateTimeOffset? DateEnd { get; set; }
    public int SwingCount { get; set; }
}
