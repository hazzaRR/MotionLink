using SQLite;

namespace MotionLink.Models;

[Table("Swings")]
public class Swing
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int SessionId { get; set; }
    [Ignore]
    public List<ImuPacket> Data { get; set; } = [];
    public double PeakRotation { get; set; }
    public double PeakGForce { get; set; }
    public DateTime? AddressTime { get; set; }
    public DateTime? TakeawayTime { get; set; }
    public DateTime? TopTime { get; set; }
    public DateTime? ImpactTime { get; set; }
    public DateTime? FinishTime { get; set; }
    public double? BackswingDurationMs { get; set; }
    public double? DownswingDurationMs { get; set; }
    public double? TempoRatio { get; set; }
}
