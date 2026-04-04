namespace MotionLink.Models;
public class ROMPacket
{    public DateTimeOffset TimeStamp {get; set; }
    public double Ax {get; set; }
    public double Ay {get; set; }
    public double Az {get; set; }
    public double Gx {get; set; }
    public double Gy {get; set; }
    public double Gz {get; set; }
}
