using SQLite;

namespace MotionLink.Models;

[Table("ImuPackets")]
public class ImuPacket
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int SwingId {get; set; }
    public DateTime TimeStamp {get; set; }
    public double Ax { get; set; } = 0;
    public double Ay {get; set; } = 0;
    public double Az {get; set; } = 0;
    public double Gx {get; set; } = 0;
    public double Gy {get; set; } = 0;
    public double Gz {get; set; } = 0;
    public double Qw {get; set; } = 0;
    public double Qx { get; set; } = 0;
    public double Qy { get; set; } = 0;
    public double Qz { get; set; } = 0;
    public bool Impact { get; set; } = false;
}
