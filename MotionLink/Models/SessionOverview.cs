namespace MotionLink.Models;

public class SessionOverview
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public int SwingCount { get; set; } = 0;
}
