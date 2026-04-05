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
}
