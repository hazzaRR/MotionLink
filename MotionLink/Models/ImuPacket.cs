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
    public double Ax {get; set; }
    public double Ay {get; set; }
    public double Az {get; set; }
    public double Gx {get; set; }
    public double Gy {get; set; }
    public double Gz {get; set; }
    public double Qw {get; set; }
    public double Qx { get; set; }
    public double Qy { get; set; }
    public double Qz { get; set; }
    public bool Impact { get; set; }
}
