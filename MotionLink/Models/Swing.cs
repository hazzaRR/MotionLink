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
    public int? ClubId { get; set; }
    public string? ClubName { get; set; }
    public double? ShaftLengthInches { get; set; }
    public bool? IsRightHanded { get; set; }
    public double? ClubheadSpeedMps { get; set; }
    public double? FaceAngleDriftDeg { get; set; }

    [Ignore]
    public string? ClubheadSpeedDisplay => ClubheadSpeedMps is double speed
        ? $"Estimated clubhead speed  {speed / 0.44704:F1} mph"
        : null;

    [Ignore]
    public string? FaceAngleDisplay => FaceAngleDriftDeg switch
    {
        > 0 => $"Face drift  {FaceAngleDriftDeg.Value:F1}° open",
        < 0 => $"Face drift  {-FaceAngleDriftDeg.Value:F1}° closed",
        0 => "Face drift  0.0°",
        _ => "Face drift unavailable"
    };
}
